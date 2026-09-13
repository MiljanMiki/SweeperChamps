// src/components/lobby/Lobby.tsx
import React, { useState, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { gamesService } from "../../services/gameService";
import "./Lobby.css";

const Lobby: React.FC = () => {
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();
  const pollRef = useRef<number | null>(null);

  const stopPolling = () => {
    if (pollRef.current !== null) {
      window.clearInterval(pollRef.current);
      pollRef.current = null;
    }
  };

  useEffect(() => {
    return () => stopPolling();
  }, []);

  const handleFindMatch = async () => {
    setError(null);
    setSearching(true);
    try {
      await gamesService.findMatch();

      // Start polling every 2s
      pollRef.current = window.setInterval(async () => {
        const pending = await gamesService.getPendingGame();
        if (pending) {
          stopPolling();
          setSearching(false);
          navigate(`/game/${pending.gameId}`);
        }
      }, 2000);
    } catch (err) {
      setSearching(false);
      setError(err instanceof Error ? err.message : "Failed to find match");
    }
  };

  const handleCancel = async () => {
    stopPolling();
    setSearching(false);
    try {
      await gamesService.cancelMatch();
    } catch {
      // Ignore cancel errors
    }
  };

  return (
    <div className="lobby">
      <h1>Minesweeper Lobby</h1>

      {error && <div className="lobby__error">{error}</div>}

      {!searching ? (
        <button className="lobby__button" onClick={handleFindMatch}>
          🎮 Find Match
        </button>
      ) : (
        <div className="lobby__searching">
          <div className="spinner" />
          <p>Searching for opponent…</p>
          <button className="lobby__cancel" onClick={handleCancel}>
            Cancel
          </button>
        </div>
      )}
    </div>
  );
};

export default Lobby;