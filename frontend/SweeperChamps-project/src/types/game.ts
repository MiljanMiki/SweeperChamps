export interface CellView {
  x: number;
  y: number;
  state: "Hidden" | "Revealed" | "Flagged";
  adjacentMineCount: number;
  isMine: boolean;
  revealedByPlayerId: number | null;
}

export interface MoveMadePayload {
  actionType: string;
  x: number;
  y: number;
  hitMine?: boolean;
  revealedCells?: { x: number; y: number; adjacentMineCount: number }[];
  wasMine?: boolean;
  playerId?: number;
}

export interface MoveMadeEvent {
  playerId: number;
  payload: MoveMadePayload;
}

export interface GameOverResult {
  playerId: number;
  score: number;
}

export interface GameSettingsDto {
  width: number;
  height: number;
  numberOfMines: number;
  startTimeSeconds: number | null;
  teamSize: number;
  winCondition: string;
  hasPowerUps: boolean;
}

export interface GamePlayerDto {
  playerId: number;
  username: string;         // ← new
  teamColor: string;
}

export interface BoardCellSnapshot {
  x: number;
  y: number;
  state: string;
  adjacentMineCount: number;
  isMine: boolean;
  revealedByPlayerId?: number | null;
}

export interface BoardStateSnapshot {
  gameId: number;
  settings: GameSettingsDto;
  players: GamePlayerDto[];
  cells: BoardCellSnapshot[];
  currentTurnPlayerId?: number | null;
  isGameOver: boolean;
  finalResults?: GameOverResult[] | null;
}

export interface ScoreEntry {              // ← new
  playerId: number;
  username: string;
  teamColor: string;
  score: number;
  isEliminated: boolean;
}