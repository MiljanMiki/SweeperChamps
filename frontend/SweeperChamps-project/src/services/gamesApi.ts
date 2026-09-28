const API_BASE_URL =
  import.meta.env.VITE_API_URL || "https://localhost:7270/api";

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