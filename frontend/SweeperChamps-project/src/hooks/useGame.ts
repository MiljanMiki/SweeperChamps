// src/hooks/useGame.ts
import { useState, useEffect, useCallback, useRef } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { buildConnection, stopConnection } from "../services/hubService";
import {
  type CellView,
  type MoveMadeEvent,
  type GameOverResult,
  type PendingGame,
  type RevealedCell,
  ActionTypes,
  HubEvents,
} from "../types/game";

interface UseGameResult {
  board: CellView[][];
  connection: HubConnection | null;
  connected: boolean;
  gameOver: boolean;
  results: GameOverResult[] | null;
  currentTurn: number | null;
  rejectionMessage: string | null;
  timeoutMessage: string | null;
  revealCell: (x: number, y: number) => Promise<void>;
  flagCell: (x: number, y: number) => Promise<void>;
}

export function useGame(game: PendingGame, token: string): UseGameResult {
  const [board, setBoard] = useState<CellView[][]>([]);
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [connected, setConnected] = useState(false);
  const [gameOver, setGameOver] = useState(false);
  const [results, setResults] = useState<GameOverResult[] | null>(null);
  const [currentTurn, setCurrentTurn] = useState<number | null>(null);
  const [rejectionMessage, setRejectionMessage] = useState<string | null>(null);
  const [timeoutMessage, setTimeoutMessage] = useState<string | null>(null);

  const connRef = useRef<HubConnection | null>(null);

  // Find which team a player belongs to
  const getTeamColor = useCallback(
    (playerId: number): "Red" | "Blue" => {
      const player = game.players.find((p) => p.playerId === playerId);
      return player?.team ?? "Red";
    },
    [game.players]
  );

  // Initialize empty board from game settings
  useEffect(() => {
    const initialBoard: CellView[][] = Array.from({ length: game.settings.rows }, (_, y) =>
      Array.from({ length: game.settings.cols }, (_, x) => ({
        state: "Hidden" as const,
        adjacentMineCount: 0,
        revealedBy: null,
        isMine: false,
        x,
        y,
      }))
    );
    setBoard(initialBoard);
  }, [game.settings.rows, game.settings.cols]);

  // Helper to update a single cell immutably
  const updateCell = useCallback(
    (x: number, y: number, patch: Partial<CellView>) => {
      setBoard((prev) => {
        const next = prev.map((row) => row.map((cell) => ({ ...cell })));
        if (next[y]?.[x]) {
          next[y][x] = { ...next[y][x], ...patch };
        }
        return next;
      });
    },
    []
  );

  // Connect + register listeners
  useEffect(() => {
    let cancelled = false;

    const connect = async () => {
      const conn = buildConnection(token);
      connRef.current = conn;

      // ---- Register listeners BEFORE starting ----

      conn.on(HubEvents.MoveMade, ({ playerId, payload }: MoveMadeEvent) => {
        const team = getTeamColor(playerId);

        if (payload.hitMine) {
          updateCell(payload.x, payload.y, {
            state: "Revealed",
            isMine: true,
            revealedBy: team,
          });
          return;
        }

        // Reveal all the cells the server told us about
        setBoard((prev) => {
          const next = prev.map((row) => row.map((cell) => ({ ...cell })));
          for (const revealed of payload.revealedCells as RevealedCell[]) {
            if (next[revealed.y]?.[revealed.x]) {
              next[revealed.y][revealed.x] = {
                ...next[revealed.y][revealed.x],
                state: "Revealed",
                adjacentMineCount: revealed.adjacentMineCount,
                revealedBy: team,
              };
            }
          }
          return next;
        });
      });

      conn.on(HubEvents.MoveRejected, (reason: string) => {
        setRejectionMessage(reason);
        setTimeout(() => setRejectionMessage(null), 3000);
      });

      conn.on(HubEvents.TurnChanged, (nextPlayerId: number) => {
        setCurrentTurn(nextPlayerId);
      });

      conn.on(HubEvents.PlayerTimeout, ({ playerId }: { playerId: number }) => {
        const player = game.players.find((p) => p.playerId === playerId);
        setTimeoutMessage(`${player?.username ?? `Player ${playerId}`} ran out of time!`);
        setTimeout(() => setTimeoutMessage(null), 3000);
      });

      conn.on(HubEvents.PlayerConnected, (playerId: number) => {
        console.log(`Player ${playerId} connected`);
      });

      conn.on(HubEvents.GameOver, (finalResults: GameOverResult[]) => {
        setResults(finalResults);
        setGameOver(true);
      });

      // ---- Start connection ----
      try {
        await conn.start();
        if (cancelled) {
          await conn.stop();
          return;
        }
        console.log("SignalR connected");
        setConnected(true);

        // Join the specific game
        await conn.invoke("JoinGame", game.gameId);
      } catch (err) {
        console.error("SignalR connection failed:", err);
      }
    };

    connect();

    return () => {
      cancelled = true;
      connRef.current?.off(HubEvents.MoveMade);
      connRef.current?.off(HubEvents.MoveRejected);
      connRef.current?.off(HubEvents.TurnChanged);
      connRef.current?.off(HubEvents.PlayerTimeout);
      connRef.current?.off(HubEvents.PlayerConnected);
      connRef.current?.off(HubEvents.GameOver);
      stopConnection();
      setConnected(false);
    };
  }, [game.gameId, token, getTeamColor, updateCell, game.players]);

  // ---- Actions ----
  const revealCell = useCallback(
    async (x: number, y: number) => {
      const conn = connRef.current;
      if (!conn || !connected || gameOver) return;

      try {
        await conn.invoke("MakeMove", game.gameId, {
          actionType: ActionTypes.Reveal,
          x,
          y,
        });
      } catch (err) {
        console.error("MakeMove (Reveal) failed:", err);
      }
    },
    [connected, gameOver, game.gameId]
  );

  const flagCell = useCallback(
    async (x: number, y: number) => {
      const conn = connRef.current;
      if (!conn || !connected || gameOver) return;

      try {
        await conn.invoke("MakeMove", game.gameId, {
          actionType: ActionTypes.Flag,
          x,
          y,
        });
      } catch (err) {
        console.error("MakeMove (Flag) failed:", err);
      }
    },
    [connected, gameOver, game.gameId]
  );

  return {
    board,
    connection,
    connected,
    gameOver,
    results,
    currentTurn,
    rejectionMessage,
    timeoutMessage,
    revealCell,
    flagCell,
  };
}