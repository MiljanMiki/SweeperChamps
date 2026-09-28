import React from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { useGame } from "../../hooks/useGame";
import Cell from "./Cell";
import "./GamePage.css";

const GamePage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const token = localStorage.getItem("token") || "";
  const myPlayerId = Number(user?.id ?? 0);

  const {
    board, settings, connected, gameOver, results, rejection,
    currentTurnPlayerId, timeoutMessage, revealCell, flagCell,
  } = useGame(token, myPlayerId);

  return (
    <div className="game-page">
      <div className="game-page__header">
        <h1>Minesweeper</h1>
        <span className={connected ? "status-connected" : "status-connecting"}>
          {connected ? "🟢 Connected" : "🟡 Connecting…"}
        </span>
      </div>

      {settings && (
        <div className="game-page__meta">
          <span>Mines: {settings.numberOfMines}</span>
          <span>Board: {settings.width}×{settings.height}</span>
          <span>Mode: {settings.winCondition}</span>
        </div>
      )}

      {rejection && <div className="toast toast--error">{rejection}</div>}
      {timeoutMessage && <div className="toast toast--warn">{timeoutMessage}</div>}
      {currentTurnPlayerId !== null && (
        <div className="turn-indicator">
          Turn: Player {currentTurnPlayerId}
          {currentTurnPlayerId === myPlayerId ? " (you)" : ""}
        </div>
      )}

      {board.length > 0 ? (
        <div className="board">
          {board.map((row, y) => (
            <div key={y} className="board-row">
              {row.map((cell, x) => (
                <Cell key={`${x}-${y}`} cell={cell} onReveal={revealCell} onFlag={flagCell} />
              ))}
            </div>
          ))}
        </div>
      ) : (
        <p>Waiting for game state…</p>
      )}

      {gameOver && (
        <div className="overlay">
          <div className="overlay__card">
            <h2>Game Over</h2>
            <ul className="overlay__results">
              {results?.map((r) => (
                <li key={r.playerId}>Player {r.playerId} — Score: {r.score}</li>
              ))}
            </ul>
            <button className="overlay__button" onClick={() => navigate("/lobby")}>
              Back to Lobby
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default GamePage;