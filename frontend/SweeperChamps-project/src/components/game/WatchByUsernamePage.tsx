import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useSpectator } from "../../hooks/useSpectator";
import { getActiveGameByUsername } from "../../services/gamesApi";
import Cell from "./Cell";
import "./GamePage.css";

const WatchByUsernamePage: React.FC = () => {
  const { username } = useParams<{ username: string }>();
  const navigate = useNavigate();
  const token = localStorage.getItem("token") || "";

  const [resolving, setResolving] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [gameId, setGameId] = useState<number | null>(null);

  useEffect(() => {
    if (!username) return;
    let cancelled = false;

    (async () => {
      try {
        setResolving(true);
        const result = await getActiveGameByUsername(username);
        if (cancelled) return;

        if (!result.hasActiveGame || result.gameId == null) {
          setError(result.message ?? `${username} is not currently in a game.`);
        } else {
          setGameId(result.gameId);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to find game.");
        }
      } finally {
        if (!cancelled) setResolving(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [username]);

  if (resolving) {
    return (
      <div className="game-page">
        <p>Looking for {username}'s game…</p>
      </div>
    );
  }

  if (error || gameId == null) {
    return (
      <div className="game-page">
        <h1>👁 Spectator</h1>
        <div className="toast toast--warn">{error ?? "No active game."}</div>
        <button className="overlay__button" onClick={() => navigate("/lobby")}>
          Back to Lobby
        </button>
      </div>
    );
  }

  return <SpectatorView gameId={gameId} username={username!} token={token} />;
};

const SpectatorView: React.FC<{
  gameId: number;
  username: string;
  token: string;
}> = ({ gameId, username, token }) => {
  const navigate = useNavigate();
  const {
    board, settings, players, scores,
    connected, gameOver, results, currentTurnPlayerId, error,
  } = useSpectator(token, gameId);

  const noop = () => {};

  return (
    <div className="game-page">
      <div className="game-page__header">
        <h1>👁 Watching {username} — Game {gameId}</h1>
        <span className={connected ? "status-connected" : "status-connecting"}>
          {connected ? "🟢 Connected" : "🟡 Connecting…"}
        </span>
      </div>

      <div className="spectator-banner">
        You are watching this game. Moves are read-only.
      </div>

      {error && <div className="toast toast--warn">{error}</div>}

      {settings && (
        <div className="game-page__meta">
          <span>Mines: {settings.numberOfMines}</span>
          <span>Board: {settings.width}×{settings.height}</span>
          <span>Mode: {settings.winCondition}</span>
        </div>
      )}

      {players.length > 0 && (
        <div className="players-panel">
          {players.map((p) => {
            const s = scores.find((x) => x.playerId === p.playerId);
            return (
              <div
                key={p.playerId}
                className={`player-card player-card--${p.teamColor.toLowerCase()} ${
                  currentTurnPlayerId === p.playerId ? "is-turn" : ""
                }`}
              >
                <div className="player-card__name">{p.username}</div>
                <div className="player-card__score">{s?.score ?? 0}</div>
                <div className="player-card__team">{p.teamColor}</div>
              </div>
            );
          })}
        </div>
      )}

      {board.length > 0 ? (
        <div className="board board--readonly">
          {board.map((row, y) => (
            <div key={y} className="board-row">
              {row.map((cell, x) => (
                <Cell
                  key={`${x}-${y}`}
                  cell={cell}
                  onReveal={noop}
                  onFlag={noop}
                  onUnflag={noop}
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

export default WatchByUsernamePage;