// src/services/api.ts
import type { LoginCredentials, RegisterCredentials, AuthResponse } from '../types';

const API_BASE_URL = 'https://localhost:7204/api';

class AuthService {
  private async request<T>(
    endpoint: string,
    method: string = 'GET',
    body?: unknown
  ): Promise<T> {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    const token = localStorage.getItem('token');
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const config: RequestInit = {
      method,
      headers,
    };

    if (body) {
      config.body = JSON.stringify(body);
    }

    const response = await fetch(`${API_BASE_URL}${endpoint}`, config);

    if (!response.ok) {
      let errorMessage = 'Something went wrong';
      try {
        const error = await response.json();
        errorMessage = error.message || error.title || JSON.stringify(error);
      } catch {
        errorMessage = response.statusText || errorMessage;
      }
      throw new Error(errorMessage);
    }

    // Handle empty responses (e.g., 204 No Content)
    const text = await response.text();
    if (!text) {
      return {} as T;
    }

    // Try to parse as JSON
    try {
      return JSON.parse(text) as T;
    } catch {
      // If it's just a plain string (like a JWT token), return it wrapped
      return { token: text } as T;
    }
  }

  async login(credentials: LoginCredentials): Promise<AuthResponse> {
    return this.request<AuthResponse>('/Auth/login', 'POST', credentials);
  }

  async register(credentials: RegisterCredentials): Promise<AuthResponse> {
    return this.request<AuthResponse>('/Auth/Register', 'POST', credentials);
  }
}

export const authService = new AuthService();