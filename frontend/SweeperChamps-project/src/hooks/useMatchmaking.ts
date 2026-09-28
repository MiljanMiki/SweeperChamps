import { useCallback, useEffect, useRef, useState } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { buildMatchmakingConnection, stopMatchmakingConnection } from "../services/matchmakingHub";

export type ConnectionState = "connecting" | "connected" | "disconnected";

interface UseMatchmakingResult {
  state: ConnectionState;
  searching: boolean;
  error: string | null;
  requestMatch: (gameSettingsId: number, isRanked: boolean) => Promise<void>;
  cancel: () => Promise<void>;
}

export function useMatchmaking(
  token: string,
  onMatchFound: () => void
): UseMatchmakingResult {
  const [state, setState] = useState<ConnectionState>("connecting");
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const connRef = useRef<HubConnection | null>(null);
  const cbRef = useRef(onMatchFound);
  useEffect(() => { cbRef.current = onMatchFound; }, [onMatchFound]);

  useEffect(() => {
    if (!token) return;
    const conn = buildMatchmakingConnection(token);
    connRef.current = conn;

    conn.on("MatchmakingStarted", () => setSearching(true));
    conn.on("MatchFound", () => {
      setSearching(false);
      cbRef.current();
    });
    conn.on("Error", (msg: string) => {
      setError(msg);
      setSearching(false);
    });

    conn.onreconnecting(() => setState("connecting"));
    conn.onreconnected(() => setState("connected"));
    conn.onclose(() => setState("disconnected"));

    let cancelled = false;
    const start = async () => {
      try {
        await conn.start();
        if (cancelled) { await conn.stop(); return; }
        setState("connected");
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : "Connection failed");
      }
    };
    start();

    return () => {
      cancelled = true;
      conn.off("MatchmakingStarted");
      conn.off("MatchFound");
      conn.off("Error");
      stopMatchmakingConnection();
    };
  }, [token]);

  const requestMatch = useCallback(async (id: number, ranked: boolean) => {
    setError(null);
    if (!connRef.current || state !== "connected") {
      setError("Not connected");
      return;
    }
    try { await connRef.current.invoke("RequestMatch", id, ranked); }
    catch (err) { setError(err instanceof Error ? err.message : "Request failed"); }
  }, [state]);

  const cancel = useCallback(async () => {
    setSearching(false);
    await stopMatchmakingConnection();
  }, []);

  return { state, searching, error, requestMatch, cancel };
}