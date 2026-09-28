import { useEffect, useRef, useState } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { buildGameConnection, stopGameConnection } from "../services/gameHub";
import type {
  CellView,
  MoveMadeEvent,
  GameOverResult,
  BoardStateSnapshot,
  GamePlayerDto,
  ScoreEntry,
} from "../types/game";

interface UseSpectatorResult {
  board: CellView[][];
  settings: BoardStateSnapshot["settings"] | null;
  players: GamePlayerDto[];
  scores: ScoreEntry[];
  connected: boolean;
  gameOver: boolean;
  results: GameOverResult[] | null;
  currentTurnPlayerId: number | null;
  error: string | null;
}

export function useSpectator(token: string, gameId: number): UseSpectatorResult {
  const [board, setBoard] = useState<CellView[][]>([]);
  const [settings, setSettings] = useState<BoardStateSnapshot["settings"] | null>(null);
  const [players, setPlayers] = useState<GamePlayerDto[]>([]);
  const [scores, setScores] = useState<ScoreEntry[]>([]);
  const [connected, setConnected] = useState(false);
  const [gameOver, setGameOver] = useState(false);
  const [results, setResults] = useState<GameOverResult[] | null>(null);
  const [currentTurnPlayerId, setCurrentTurnPlayerId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const connRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    if (!token || !gameId) return;
    let cancelled = false;

    const conn = buildGameConnection(token);
    connRef.current = conn;

    conn.on("BoardState", (snap: BoardStateSnapshot) => {
      const grid: CellView[][] = Array.from({ length: snap.settings.height }, (_, y) =>
        Array.from({ length: snap.settings.width }, (_, x) => ({
          x, y,
          state: "Hidden",
          adjacentMineCount: 0,
          isMine: false,
          revealedByPlayerId: null,
        }))
      );
      for (const c of snap.cells) {
        grid[c.y][c.x] = {
          x: c.x,
          y: c.y,
          state: c.state as CellView["state"],
          adjacentMineCount: c.adjacentMineCount,
          isMine: c.isMine,
          revealedByPlayerId: c.revealedByPlayerId ?? null,
        };
      }
      setBoard(grid);
      setSettings(snap.settings);
      setPlayers(snap.players ?? []);
      setCurrentTurnPlayerId(snap.currentTurnPlayerId ?? null);
      setGameOver(!!snap.isGameOver);
      if (snap.finalResults) setResults(snap.finalResults);
    });

    conn.on("ScoreUpdate", (update: ScoreEntry[]) => setScores(update));

    conn.on("MoveMade", (evt: MoveMadeEvent) => {
      const p = evt.payload;
      setBoard((prev) => {
        if (!prev.length) return prev;
        const next = prev.map((row) => row.map((c) => ({ ...c })));
        if (p.hitMine) {
          next[p.y][p.x] = { ...next[p.y][p.x], state: "Revealed", isMine: true, revealedByPlayerId: evt.playerId };
          return next;
        }
        if (p.revealedCells && p.revealedCells.length > 0) {
          for (const rc of p.revealedCells) {
            next[rc.y][rc.x] = {
              ...next[rc.y][rc.x],
              state: "Revealed",
              adjacentMineCount: rc.adjacentMineCount,
              revealedByPlayerId: evt.playerId,
            };
          }
          return next;
        }
        if (p.actionType === "Flag") {
          next[p.y][p.x] = { ...next[p.y][p.x], state: "Flagged" };
        } else if (p.actionType === "Unflag") {
          next[p.y][p.x] = { ...next[p.y][p.x], state: "Hidden" };
        }
        return next;
      });
    });

    conn.on("MoveRejected", (reason: string) => {
      console.warn("[Spectator] Move rejected:", reason);
      setError(reason);
    });

    conn.on("TurnChanged", (nextPlayerId: number) => setCurrentTurnPlayerId(nextPlayerId));

    conn.on("GameOver", (final: GameOverResult[]) => {
      setResults(final);
      setGameOver(true);
    });

    const start = async () => {
      try {
        await conn.start();
        if (cancelled) { await conn.stop(); return; }
        setConnected(true);
        await conn.invoke("JoinGameAsSpectator", gameId);
      } catch (err) {
        if (!cancelled) {
          console.error("[Spectator] Connection failed:", err);
          setError(err instanceof Error ? err.message : "Connection failed");
        }
      }
    };
    start();

    return () => {
      cancelled = true;
      ["BoardState", "ScoreUpdate", "MoveMade", "MoveRejected", "TurnChanged", "GameOver", "SpectatorJoined"]
        .forEach((e) => conn.off(e));
      stopGameConnection();
      setConnected(false);
    };
  }, [token, gameId]);

  return {
    board, settings, players, scores,
    connected, gameOver, results, currentTurnPlayerId, error,
  };
}