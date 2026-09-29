export interface UserDto {
  usersId: number;
  username: string;
  email: string;
  datecreated: string;   // "2026-09-29" (DateOnly iz C#)
  elo: number;
  userRole: string;      // "User" | "Admin" | "NotSet"
}

export interface GamePlayerDto {
  playerId: number;
  gameId: number;
  teamColor: string | number;
  score: number;
  outcome?: string | number;
}

export interface GameSummaryDto {
  gamesId: number;
  startTime: string;
  endTime: string | null;
  status: string | number;
  score: number;
}

export interface MatchHistoryEntry {
  gamePlayer: GamePlayerDto;
  game: GameSummaryDto;
}

export interface UserProfile {
  user: UserDto;
  history: MatchHistoryEntry[];
  totalGames: number;
  wins: number;
  losses: number;
  draws: number;
  winRate: number;
}
