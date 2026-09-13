// src/types/index.ts
export interface User {
  id: string;
  username: string;
  email: string;
  slikaURL?: string;
}

export interface AuthResponse {
  user?: User;
  token?: string;
  // Sometimes backend returns these directly
  id?: string;
  username?: string;
  email?: string;
  slikaURL?: string;
}

export interface LoginCredentials {
  username: string;
  password: string;
}

export interface RegisterCredentials {
  username: string;
  email: string;
  password: string;
  slikaURL: string;
}