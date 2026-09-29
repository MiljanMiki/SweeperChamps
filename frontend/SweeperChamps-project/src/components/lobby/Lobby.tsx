import React, { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { useMatchmaking } from "../../hooks/useMatchmaking";
import { checkHasActiveGame } from "../../services/gamesApi";
import "./Lobby.css";

const GAME_SETTINGS_ID = 6;
const IS_RANKED = false;

const Lobby: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const token = localStorage.getItem("token") || "";

  // ── Da li je korisnik već u aktivnoj partiji? ──
  const [checkingActive, setCheckingActive] = useState(true);
  const [hasActiveGame, setHasActiveGame] = useState(false);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const active = await checkHasActiveGame();
      if (!cancelled) {
        setHasActiveGame(active);
        setCheckingActive(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const handleMatchFound = useCallback(() => {
    navigate("/game");
  }, [navigate]);

  const { state, searching, error, requestMatch, cancel } = useMatchmaking(
    token,
    handleMatchFound
  );

  const handleReconnect = () => {
    navigate("/game");
  };

  // Dok proveravamo, ne prikazuj ništa što bi zbunilo
  if (checkingActive) {
    return (
      <div className="lobby">
        <h1>Minesweeper Lobby</h1>
        <p>Checking for active game…</p>
      </div>
    );
  }

  return (
    <div className="lobby">
      <h1>Minesweeper Lobby</h1>

      <div className={`lobby__connection lobby__connection--${state}`}>
        {state === "connected" && "🟢 Connected"}
        {state === "connecting" && "🟡 Connecting…"}
        {state === "disconnected" && "🔴 Disconnected"}
      </div>

      <p>
          Welcome,{" "}
          <span
            className="lobby__profile-link"
            onClick={() => navigate(`/profile/${user?.username}`)}
          >
            {user?.username}
          </span>
        </p>

      {error && <div className="lobby__error">{error}</div>}

      {hasActiveGame ? (
        // ── Već u partiji → prikaži Reconnect ──
        <div className="lobby__reconnect">
          <p className="lobby__reconnect-info">
            You have a game in progress.
          </p>
          <button className="lobby__button lobby__button--reconnect" onClick={handleReconnect}>
            🔄 Reconnect
          </button>
        </div>
      ) : !searching ? (
        // ── Nema partije → prikaži Find Match ──
        <button
          className="lobby__button"
          onClick={() => requestMatch(GAME_SETTINGS_ID, IS_RANKED)}
          disabled={state !== "connected"}
        >
          🎮 Find Match
        </button>
      ) : (
        <div className="lobby__searching">
          <div className="spinner" />
          <p>Searching for opponent…</p>
          <button className="lobby__cancel" onClick={cancel}>
            Cancel
          </button>
        </div>
      )}
    </div>
  );
};

export default Lobby;