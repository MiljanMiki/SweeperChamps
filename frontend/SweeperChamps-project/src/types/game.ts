// src/types/game.ts
export interface CellView {
  state: "Hidden" | "Revealed" | "Flagged";
  adjacentMineCount: number;
  revealedBy: "Red" | "Blue" | null;
  isMine: boolean;
  x: number;
  y: number;
}

export interface Player {
  playerId: number;
  username: string;
  team: "Red" | "Blue";
  score: number;
}

export interface RevealedCell {
  x: number;
  y: number;
  adjacentMineCount: number;
}

export interface MovePayload {
  revealedCells: RevealedCell[];
  hitMine: boolean;
  x: number;
  y: number;
}

export interface MoveMadeEvent {
  playerId: number;
  payload: MovePayload;
}

export interface GameOverResult {
  playerId: number;
  score: number;
}

export interface GameSettings {
  rows: number;
  cols: number;
  mineCount: number;
  mode: "Classic" | "TimeRush" | "Team";
}

export interface PendingGame {
  gameId: number;
  players: Player[];
  settings: GameSettings;
}

export const ActionTypes = {
  Reveal: "Reveal",
  Flag: "Flag",
} as const;

export const HubEvents = {
  MoveMade: "MoveMade",
  MoveRejected: "MoveRejected",
  TurnChanged: "TurnChanged",
  PlayerTimeout: "PlayerTimeout",
  PlayerConnected: "PlayerConnected",
  GameOver: "GameOver",
} as const;