import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getUserProfile } from "../../services/userProfileApi";
import type { UserProfile } from "../../types/profile";
import "./ProfilePage.css";

// ── Enum → string maps ──
// C# GameStatuses { Finished=0, InProgress=1, Aborted=2, Terminated=3 }
const GAME_STATUS_LABELS: Record<number, string> = {
  0: "Finished",
  1: "InProgress",
  2: "Aborted",
  3: "Terminated",
};

// C# Outcomes { Pending=0, Win=1, Loss=2, Draw=3 } — prilagodi ako je drugi redosled
const OUTCOME_LABELS: Record<number, string> = {
  0: "Pending",
  1: "Win",
  2: "Loss",
  3: "Draw",
};

// C# TeamColors { Red=0, Blue=1 }
const TEAM_COLOR_LABELS: Record<number, string> = {
  0: "Red",
  1: "Blue",
};

function statusToString(s: any): string {
  if (typeof s === "number") return GAME_STATUS_LABELS[s] ?? "Unknown";
  if (typeof s === "string") return s;
  return "Unknown";
}

function outcomeToString(o: any): string {
  if (typeof o === "number") return OUTCOME_LABELS[o] ?? "Pending";
  if (typeof o === "string") return o;
  return "Pending";
}

function teamColorToString(tc: any): string {
  if (typeof tc === "number") return TEAM_COLOR_LABELS[tc] ?? "Unknown";
  if (typeof tc === "string") return tc;
  return "Unknown";
}

const ProfilePage: React.FC = () => {
  const { username } = useParams<{ username: string }>();
  const navigate = useNavigate();
  const { user: me } = useAuth();

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!username) return;
    let cancelled = false;

    (async () => {
      try {
        setLoading(true);
        const data = await getUserProfile(username);
        if (!cancelled) {
          setProfile(data);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load profile");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [username]);

  const fmtDateOnly = (s: string) => {
    const d = new Date(s + "T00:00:00");
    return d.toLocaleDateString("en-GB", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    });
  };

  const fmtDateTime = (s: string) =>
    new Date(s).toLocaleString("en-GB", {
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });

  const fmtDuration = (start: string, end: string | null) => {
    if (!end) return "—";
    const sec = Math.max(
      0,
      Math.floor((new Date(end).getTime() - new Date(start).getTime()) / 1000)
    );
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return m > 0 ? `${m}m ${s}s` : `${s}s`;
  };

  const isMe = me?.username === username;

  if (loading) {
    return (
      <div className="profile-page">
        <p>Loading profile…</p>
      </div>
    );
  }

  if (error || !profile) {
    return (
      <div className="profile-page">
        <h1>Profile</h1>
        <div className="profile-page__error">
          {error ?? "Profile not found."}
        </div>
        <button
          className="profile-page__back"
          onClick={() => navigate("/lobby")}
        >
          ← Back to Lobby
        </button>
      </div>
    );
  }

  const { user, history } = profile;

  return (
    <div className="profile-page">
      <div className="profile-page__header">
        <div className="profile-avatar">
          {user.username.charAt(0).toUpperCase()}
        </div>
        <div className="profile-identity">
          <h1>
            {user.username}
            {isMe && <span className="you-badge">YOU</span>}
            {user.userRole === "Admin" && (
              <span className="admin-badge">ADMIN</span>
            )}
          </h1>
          <p className="profile-meta">
            Elo: <strong>{user.elo ?? 0}</strong> · Member since{" "}
            {fmtDateOnly(user.datecreated)}
          </p>
        </div>
      </div>

      <div className="profile-stats">
        <div className="stat-card">
          <div className="stat-card__value">{profile.totalGames}</div>
          <div className="stat-card__label">Games</div>
        </div>
        <div className="stat-card stat-card--win">
          <div className="stat-card__value">{profile.wins}</div>
          <div className="stat-card__label">Wins</div>
        </div>
        <div className="stat-card stat-card--loss">
          <div className="stat-card__value">{profile.losses}</div>
          <div className="stat-card__label">Losses</div>
        </div>
        <div className="stat-card">
          <div className="stat-card__value">{profile.draws}</div>
          <div className="stat-card__label">Draws</div>
        </div>
        <div className="stat-card stat-card--rate">
          <div className="stat-card__value">{profile.winRate}%</div>
          <div className="stat-card__label">Win Rate</div>
        </div>
      </div>

      <h2 className="profile-history-title">Match History</h2>

      {history.length === 0 ? (
        <div className="profile-empty">
          <p>No matches played yet.</p>
        </div>
      ) : (
        <table className="match-history-table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Game</th>
              <th>Color</th>
              <th>Score</th>
              <th>Duration</th>
              <th>Result</th>
            </tr>
          </thead>
          <tbody>
            {history.map((h) => {
              const outcome = outcomeToString(h.gamePlayer.outcome);
              const status = statusToString(h.game.status);
              const teamColor = teamColorToString(h.gamePlayer.teamColor);

              const resultClass =
                outcome === "Win"
                  ? "win"
                  : outcome === "Loss"
                  ? "loss"
                  : outcome === "Draw"
                  ? "draw"
                  : status === "InProgress"
                  ? "inprogress"
                  : "pending";

              const resultLabel =
                outcome === "Win"
                  ? "WIN"
                  : outcome === "Loss"
                  ? "LOSS"
                  : outcome === "Draw"
                  ? "DRAW"
                  : status === "InProgress"
                  ? "…"
                  : status.toUpperCase();

              return (
                <tr key={h.game.gamesId}>
                  <td>{fmtDateTime(h.game.startTime)}</td>
                  <td>
                    <strong>#{h.game.gamesId}</strong>
                  </td>
                  <td>
                    <span
                      className={`team-dot team-dot--${teamColor.toLowerCase()}`}
                    />
                    {teamColor}
                  </td>
                  <td>{h.gamePlayer.score}</td>
                  <td>{fmtDuration(h.game.startTime, h.game.endTime)}</td>
                  <td>
                    <span className={`result-pill result-pill--${resultClass}`}>
                      {resultLabel}
                    </span>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}

      <button
        className="profile-page__back"
        onClick={() => navigate("/lobby")}
      >
        ← Back to Lobby
      </button>
    </div>
  );
};

export default ProfilePage;