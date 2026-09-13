// src/services/gamesService.ts
import type { PendingGame } from "../types/game";

const API_BASE_URL = import.meta.env.REACT_APP_API_URL || "http://localhost:7204/api";

class GamesService {
  private async request<T>(endpoint: string, method = "GET", body?: unknown): Promise<T> {
    const headers: HeadersInit = { "Content-Type": "application/json" };
    const token = localStorage.getItem("token");
    if (token) headers["Authorization"] = `Bearer ${token}`;

    const config: RequestInit = { method, headers };
    if (body) config.body = JSON.stringify(body);

    const response = await fetch(`${API_BASE_URL}${endpoint}`, config);
    if (!response.ok) {
      const text = await response.text();
      throw new Error(text || response.statusText);
    }
    const text = await response.text();
    return text ? (JSON.parse(text) as T) : ({} as T);
  }

  // Ask the API to find a match for me
  async findMatch(): Promise<{ ticketId: string }> {
    return this.request("/games/find-match", "POST");
  }

  // Poll this until it returns a real game (or 404 while waiting)
  async getPendingGame(): Promise<PendingGame | null> {
    try {
      const response = await fetch(`${API_BASE_URL}/games/pending`, {
        headers: {
          Authorization: `Bearer ${localStorage.getItem("token")}`,
        },
      });
      if (response.status === 404) return null;
      if (!response.ok) throw new Error(response.statusText);
      return await response.json();
    } catch {
      return null;
    }
  }

  async cancelMatch(): Promise<void> {
    await this.request("/games/cancel-match", "POST");
  }

  async getGame(gameId: number): Promise<PendingGame> {
    return this.request(`/games/${gameId}`);
  }
}

export const gamesService = new GamesService();