// src/types/index.ts
export interface User {
  id: string;
  username: string;
  email: string;
}

export interface AuthResponse {
  user: User;
  token: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface GameState {
  board: number[][];
  revealed: boolean[][];
  flagged: boolean[][];
  gameOver: boolean;
  won: boolean;
  mines: number;
  rows: number;
  cols: number;
}