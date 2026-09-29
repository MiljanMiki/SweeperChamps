const API_BASE_URL =
  import.meta.env.VITE_API_URL || "https://localhost:7270/api";

export interface AdminActiveGamePlayer {
  playerId: number;
  username: string;
  teamColor: string;
}

export interface AdminActiveGame {
  gameId: number;
  playerCount: number;
  players: AdminActiveGamePlayer[];
}

export async function getAdminActiveGames(): Promise<AdminActiveGame[]> {
  const token = localStorage.getItem("token");
  if (!token) throw new Error("Not authenticated");

  const res = await fetch(`${API_BASE_URL}/Admin/active-games`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (res.status === 401 || res.status === 403) {
    throw new Error("You are not authorized to view this page.");
  }
  if (!res.ok) {
    throw new Error(await res.text() || res.statusText);
  }
  return res.json();
}