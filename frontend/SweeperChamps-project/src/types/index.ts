export interface User {
  id: string;
  username: string;
  email: string;
  role: string;   // "Admin" | "User"
}

export interface AuthResponse {
  user?: User;
  token?: string;
  id?: string;
  username?: string;
  email?: string;
  role?: string;
}

export interface LoginCredentials {
  username: string;
  password: string;
}

export interface RegisterCredentials {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
}