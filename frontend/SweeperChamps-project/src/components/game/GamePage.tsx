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
    board, settings, players, scores,
    connected, gameOver, results, rejection,
    currentTurnPlayerId, timeoutMessage,
    revealCell, flagCell, unflagCell,
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

      {/* ── Players + live scores ── */}
      {players.length > 0 && (
        <div className="players-panel">
          {players.map((p) => {
            const s = scores.find((x) => x.playerId === p.playerId);
            const isMe = p.playerId === myPlayerId;
            const isTheirTurn = currentTurnPlayerId === p.playerId;
            return (
              <div
                key={p.playerId}
                className={`player-card player-card--${p.teamColor.toLowerCase()} ${
                  isTheirTurn ? "is-turn" : ""
                }`}
              >
                <div className="player-card__name">
                  {p.username} {isMe && <span className="you-badge">(you)</span>}
                </div>
                <div className="player-card__score">
                  {s ? s.score : 0}
                  {s?.isEliminated && <span className="eliminated-badge">☠</span>}
                </div>
                <div className="player-card__team">{p.teamColor}</div>
              </div>
            );
          })}
        </div>
      )}

      {rejection && <div className="toast toast--error">{rejection}</div>}
      {timeoutMessage && <div className="toast toast--warn">{timeoutMessage}</div>}

      {board.length > 0 ? (
        <div className="board">
          {board.map((row, y) => (
            <div key={y} className="board-row">
              {row.map((cell, x) => (
                <Cell
                  key={`${x}-${y}`}
                  cell={cell}
                  onReveal={revealCell}
                  onFlag={flagCell}
                  onUnflag={unflagCell}
                />
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
              {results?.map((r) => {
                const p = players.find((x) => x.playerId === r.playerId);
                return (
                  <li key={r.playerId}>
                    {p?.username ?? `Player ${r.playerId}`} — {r.score}
                  </li>
                );
              })}
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