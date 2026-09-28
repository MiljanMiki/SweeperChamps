import React, { useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { useMatchmaking } from "../../hooks/useMatchmaking";
import "./Lobby.css";

const GAME_SETTINGS_ID = 6; // ⚠️ your existing settings row
const IS_RANKED = false;

const Lobby: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const token = localStorage.getItem("token") || "";

  const handleMatchFound = useCallback(() => {
    navigate("/game");
  }, [navigate]);

  const { state, searching, error, requestMatch, cancel } = useMatchmaking(token, handleMatchFound);

  return (
    <div className="lobby">
      <h1>Minesweeper Lobby</h1>
      <div className={`lobby__connection lobby__connection--${state}`}>
        {state === "connected" && "🟢 Connected"}
        {state === "connecting" && "🟡 Connecting…"}
        {state === "disconnected" && "🔴 Disconnected"}
      </div>
      <p>Welcome, {user?.username}</p>
      {error && <div className="lobby__error">{error}</div>}

      {!searching ? (
        <button className="lobby__button" onClick={() => requestMatch(GAME_SETTINGS_ID, IS_RANKED)} disabled={state !== "connected"}>
          🎮 Find Match
        </button>
      ) : (
        <div className="lobby__searching">
          <div className="spinner" />
          <p>Searching for opponent…</p>
          <button className="lobby__cancel" onClick={cancel}>Cancel</button>
        </div>
      )}
    </div>
  );
};

export default Lobby;