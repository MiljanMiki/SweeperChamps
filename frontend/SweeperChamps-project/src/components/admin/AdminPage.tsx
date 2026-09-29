import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getAdminActiveGames, type AdminActiveGame } from "../../services/adminApi";
import "./AdminPage.css";

const AdminPage: React.FC = () => {
  const { isAdmin, user } = useAuth();
  const navigate = useNavigate();

  const [games, setGames] = useState<AdminActiveGame[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastRefresh, setLastRefresh] = useState<Date>(new Date());

  const fetchGames = async () => {
    try {
      const data = await getAdminActiveGames();
      setGames(data);
      setError(null);
      setLastRefresh(new Date());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load games");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!isAdmin) return;
    fetchGames();
    const interval = setInterval(fetchGames, 3000);
    return () => clearInterval(interval);
  }, [isAdmin]);

  if (!isAdmin) {
    return (
      <div className="admin-page">
        <h1>Access Denied</h1>
        <p>You must be an admin to view this page.</p>
      </div>
    );
  }

  return (
    <div className="admin-page">
      <div className="admin-page__header">
        <h1>🛡 Admin Panel</h1>
        <div className="admin-page__meta">
          <span>{games.length} active game{games.length !== 1 ? "s" : ""}</span>
          <span className="admin-page__refresh">
            Last refresh: {lastRefresh.toLocaleTimeString()}
          </span>
        </div>
      </div>

      <p className="admin-page__user">
        Logged in as <strong>{user?.username}</strong> ({user?.role})
      </p>

      {error && <div className="admin-page__error">{error}</div>}

      {loading ? (
        <p>Loading…</p>
      ) : games.length === 0 ? (
        <div className="admin-page__empty">
          <p>No active games right now.</p>
        </div>
      ) : (
        <table className="admin-table">
          <thead>
            <tr>
              <th>Game ID</th>
              <th>Players</th>
              <th>Count</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {games.map((g) => (
              <tr key={g.gameId}>
                <td>
                  <strong>#{g.gameId}</strong>
                </td>
                <td>
                  <ul className="admin-table__players">
                    {g.players.map((p) => (
                      <li key={p.playerId}>
                        <span className={`team-dot team-dot--${p.teamColor.toLowerCase()}`} />
                        {p.username}
                      </li>
                    ))}
                  </ul>
                </td>
                <td>{g.playerCount}</td>
                <td>
                  <button
                    className="admin-table__watch"
                    onClick={() => navigate(`/game/${g.players[0]?.username}`)}
                    disabled={!g.players[0]}
                  >
                    👁 Watch
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default AdminPage;