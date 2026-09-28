const API_BASE_URL =
  import.meta.env.VITE_API_URL || "https://localhost:7270/api";

/**
 * Proverava da li trenutno ulogovani korisnik ima aktivnu partiju.
 * Vraća true ako ima, false u svim ostalim slučajevima (nema tokena,
 * API nedostupan, itd).
 */
export async function checkHasActiveGame(): Promise<boolean> {
  const token = localStorage.getItem("token");
  if (!token) return false;

  try {
    const res = await fetch(`${API_BASE_URL}/Games/active`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return false;
    const data = (await res.json()) as { hasActiveGame: boolean };
    return data.hasActiveGame === true;
  } catch {
    return false;
  }
}

/**
 * Vraća aktivnu partiju korisnika sa datim username-om.
 * Koristi se za /game/:username (spectator mode).
 */
export interface ActiveGameResponse {
  hasActiveGame: boolean;
  gameId: number | null;
  username: string;
  message?: string;
}

export async function getActiveGameByUsername(
  username: string
): Promise<ActiveGameResponse> {
  const token = localStorage.getItem("token");
  const res = await fetch(
    `${API_BASE_URL}/Games/active/by-username/${encodeURIComponent(username)}`,
    {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    }
  );
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || res.statusText);
  }
  return res.json();
}