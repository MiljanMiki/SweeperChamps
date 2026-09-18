// src/components/game/GamePage.tsx
import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { gamesService } from "../../services/gameService";
import { useGame } from "../../hooks/useGame";
import GameBoard from "./GameBoard";
import type { PendingGame } from "../../types/game";
import "./GamePage.css";

const GamePage: React.FC = () => {
  const { gameId } = useParams<{ gameId: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();
  const token = localStorage.getItem("token") || "";

  const [game, setGame] = useState<PendingGame | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Load game details
  useEffect(() => {
    const load = async () => {
      if (!gameId) return;
      try {
        setLoading(true);
        const g = await gamesService.getGame(Number(gameId));
        setGame(g);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load game");
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [gameId]);

  if (loading) return <div className="game-page__loading">Loading game…</div>;
  if (error) return <div className="game-page__error">Error: {error}</div>;
  if (!game) return <div className="game-page__error">Game not found</div>;

  return <GameView game={game} token={token} currentUserId={user?.id} />;
};

// Inner component so hooks run only after game is loaded
const GameView: React.FC<{
  game: PendingGame;
  token: string;
  currentUserId?: string;
}> = ({ game, token, currentUserId }) => {
  const {
    board,
    connected,
    gameOver,
    results,
    currentTurn,
    rejectionMessage,
    timeoutMessage,
    revealCell,
    flagCell,
  } = useGame(game, token);

  const navigate = useNavigate();

  return (
    <div className="game-page">
      <div className="game-page__header">
        <h1>Minesweeper Match</h1>
        <div className={`connection-status ${connected ? "is-connected" : "is-connecting"}`}>
          {connected ? "🟢 Connected" : "🟡 Connecting…"}
        </div>
      </div>

      <div className="game-page__players">
        {game.players.map((p) => (
          <div
            key={p.playerId}
            className={`player-card player-card--${p.team.toLowerCase()} ${
              currentTurn === p.playerId ? "is-active" : ""
            }`}
          >
            <span className="player-card__name">{p.username}</span>
            <span className="player-card__team">{p.team}</span>
            {results && (
              <span className="player-card__score">
                Score: {results.find((r) => r.playerId === p.playerId)?.score ?? 0}
              </span>
            )}
          </div>
        ))}
      </div>

      {rejectionMessage && (
        <div className="game-page__toast game-page__toast--error">
          {rejectionMessage}
        </div>
      )}
      {timeoutMessage && (
        <div className="game-page__toast game-page__toast--warning">
          {timeoutMessage}
        </div>
      )}

      <GameBoard board={board} onReveal={revealCell} onFlag={flagCell} />

      {gameOver && (
        <div className="game-over-overlay">
          <div className="game-over-card">
            <h2>Game Over!</h2>
            <ul className="game-over__results">
              {results
                ?.slice()
                .sort((a, b) => b.score - a.score)
                .map((r) => {
                  const player = game.players.find((p) => p.playerId === r.playerId);
                  return (
                    <li key={r.playerId}>
                      <span>{player?.username ?? `Player ${r.playerId}`}</span>
                      <span>{r.score}</span>
                    </li>
                  );
                })}
            </ul>
            <button onClick={() => navigate("/lobby")} className="game-over__button">
              Back to Lobby
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default GamePage;