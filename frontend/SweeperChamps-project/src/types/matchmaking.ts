export interface MatchmakingEvents {
  MatchmakingStarted: () => void;
  MatchFound: (gameId: number) => void;
  Error: (message: string) => void;
}