import { useCallback, useEffect, useRef, useState } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { buildGameConnection, stopGameConnection } from "../services/gameHub";
import type {
  CellView,
  MoveMadeEvent,
  GameOverResult,
  BoardStateSnapshot,
} from "../types/game";

interface UseGameResult {
  board: CellView[][];
  settings: BoardStateSnapshot["settings"] | null;
  myPlayerId: number;
  connected: boolean;
  gameOver: boolean;
  results: GameOverResult[] | null;
  rejection: string | null;
  currentTurnPlayerId: number | null;
  timeoutMessage: string | null;
  revealCell: (x: number, y: number) => Promise<void>;
  flagCell: (x: number, y: number) => Promise<void>;
}

export function useGame(
  token: string,
  myPlayerId: number
): UseGameResult {
  const [board, setBoard] = useState<CellView[][]>([]);
  const [settings, setSettings] = useState<BoardStateSnapshot["settings"] | null>(null);
  const [connected, setConnected] = useState(false);
  const [gameOver, setGameOver] = useState(false);
  const [results, setResults] = useState<GameOverResult[] | null>(null);
  const [rejection, setRejection] = useState<string | null>(null);
  const [currentTurnPlayerId, setCurrentTurnPlayerId] = useState<number | null>(null);
  const [timeoutMessage, setTimeoutMessage] = useState<string | null>(null);
  const connRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    if (!token) return;
    let cancelled = false;

    const conn = buildGameConnection(token);
    connRef.current = conn;

    // ── Snapshot from server ──
    conn.on("BoardState", (snap: BoardStateSnapshot) => {
      const rows = snap.settings.height;
      const cols = snap.settings.width;
      const grid: CellView[][] = Array.from({ length: rows }, (_, y) =>
        Array.from({ length: cols }, (_, x) => ({
          x,
          y,
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
      setCurrentTurnPlayerId(snap.currentTurnPlayerId ?? null);
      setGameOver(!!snap.isGameOver);
      if (snap.finalResults) setResults(snap.finalResults);
    });

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
      setRejection(reason);
      setTimeout(() => setRejection(null), 3000);
    });

    conn.on("TurnChanged", (nextPlayerId: number) => setCurrentTurnPlayerId(nextPlayerId));

    conn.on("PlayerTimeout", (payload: { playerId: number }) => {
      setTimeoutMessage(`Player ${payload.playerId} ran out of time`);
      setTimeout(() => setTimeoutMessage(null), 3000);
    });

    conn.on("GameOver", (final: GameOverResult[]) => {
      setResults(final);
      setGameOver(true);
    });

    const start = async () => {
      try {
        await conn.start();
        if (cancelled) { await conn.stop(); return; }
        setConnected(true);
        console.log("[Game] Connected");
        await conn.invoke("JoinGame");
        console.log("[Game] JoinGame invoked");
      } catch (err) {
        console.error("[Game] Connection failed:", err);
      }
    };
    start();

    return () => {
      cancelled = true;
      ["BoardState","MoveMade","MoveRejected","TurnChanged","PlayerTimeout","PlayerConnected","GameOver"]
        .forEach((e) => conn.off(e));
      stopGameConnection();
      setConnected(false);
    };
  }, [token]);

  const revealCell = useCallback(async (x: number, y: number) => {
    const conn = connRef.current;
    if (!conn || !connected || gameOver) return;
    try {
      await conn.invoke("MakeMove", { actionType: "Reveal", x, y });
    } catch (err) {
      console.error("[Game] Reveal failed:", err);
    }
  }, [connected, gameOver]);

  const flagCell = useCallback(async (x: number, y: number) => {
    const conn = connRef.current;
    if (!conn || !connected || gameOver) return;
    try {
      await conn.invoke("MakeMove", { actionType: "Flag", x, y });
    } catch (err) {
      console.error("[Game] Flag failed:", err);
    }
  }, [connected, gameOver]);

  return {
    board, settings, myPlayerId,
    connected, gameOver, results, rejection,
    currentTurnPlayerId, timeoutMessage,
    revealCell, flagCell,
  };
}