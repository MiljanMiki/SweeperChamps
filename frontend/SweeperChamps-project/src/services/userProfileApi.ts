import type {
  UserDto,
  MatchHistoryEntry,
  UserProfile,
} from "../types/profile";

const API_BASE_URL =
  import.meta.env.VITE_API_URL || "https://localhost:7270/api";

async function fetchJson<T>(url: string): Promise<T> {
  const res = await fetch(url);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || res.statusText);
  }
  return res.json();
}

export async function getUserByUsername(username: string): Promise<UserDto> {
  return fetchJson<UserDto>(
    `${API_BASE_URL}/Users/by-username/${encodeURIComponent(username)}`
  );
}

export async function getMatchHistory(
  playerId: number,
  page: number = 0,
  pageSize: number = 50
): Promise<MatchHistoryEntry[]> {
  const url =
    `${API_BASE_URL}/GamePlayers/match-history` +
    `?playerID=${playerId}&page=${page}&pageSize=${pageSize}`;
  return fetchJson<MatchHistoryEntry[]>(url);
}

export async function getUserProfile(username: string): Promise<UserProfile> {
  const user = await getUserByUsername(username);
  const history = await getMatchHistory(user.usersId, 0, 100);

  // Ako Outcome nije u DTO, sve partije koje NISU InProgress smatramo završenim.
  // Ako želiš preciznije, dodaj Outcome u GamePlayerDto na backendu.
  const completed = history.filter((h) => {
    const outcome = h.gamePlayer.outcome;
    if (outcome) return outcome !== "Pending";
    // Fallback: koristi status partije
    return h.game.status === "Finished" || h.game.status === "Terminated";
  });

  const wins = completed.filter((h) => h.gamePlayer.outcome === "Win").length;
  const losses = completed.filter((h) => h.gamePlayer.outcome === "Loss").length;
  const draws = completed.filter((h) => h.gamePlayer.outcome === "Draw").length;

  // Ako Outcome nije dostupan, izračunaj Win/Loss iz WinningTeam-a
  let realWins = wins;
  let realLosses = losses;
  if (wins === 0 && losses === 0 && completed.length > 0) {
    // grubi fallback — sve završene partije broji kao pobede ako je score veći od 0
    realWins = completed.filter((h) => h.gamePlayer.score > 0).length;
    realLosses = completed.length - realWins - draws;
  }

  const totalGames = completed.length;
  const winRate =
    totalGames > 0 ? Math.round((realWins / totalGames) * 100) : 0;

  return {
    user,
    history,
    totalGames,
    wins: realWins,
    losses: realLosses,
    draws,
    winRate,
  };
}