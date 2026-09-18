// src/context/AuthContext.tsx
import React, { createContext, useState, useEffect, useContext } from 'react';
import type { User, LoginCredentials, RegisterCredentials, AuthResponse } from '../types';
import { authService } from '../services/api';

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  register: (credentials: RegisterCredentials) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: React.ReactNode;
}

// Helper to extract user + token from various response shapes
function parseAuthResponse(response: AuthResponse, fallbackUsername: string): { user: User; token: string } {
  // Case 1: { user: {...}, token: "..." }
  if (response.user && response.token) {
    return { user: response.user, token: response.token };
  }

  // Case 2: Response IS the user, and token might be in a header we don't have
  if (response.username) {
    const user: User = {
      id: response.id || response.username,
      username: response.username,
      email: response.email || '',
      slikaURL: response.slikaURL,
    };
    return { user, token: response.token || `token-${Date.now()}` };
  }

  // Case 3: Response is just a token string (already wrapped by api service)
  if (response.token && !response.user) {
    const user: User = {
      id: fallbackUsername,
      username: fallbackUsername,
      email: '',
    };
    return { user, token: response.token };
  }

  // Fallback: use the username we sent
  const user: User = {
    id: fallbackUsername,
    username: fallbackUsername,
    email: '',
  };
  return { user, token: `token-${Date.now()}` };
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('token');
    const storedUser = localStorage.getItem('user');

    if (token && storedUser) {
      try {
        setUser(JSON.parse(storedUser));
      } catch {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
      }
    }
    setLoading(false);
  }, []);

  const login = async (credentials: LoginCredentials) => {
    setLoading(true);
    try {
      const response = await authService.login(credentials);
      console.log('Login response:', response); // Debug log

      const { user: parsedUser, token } = parseAuthResponse(response, credentials.username);

      setUser(parsedUser);
      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(parsedUser));
    } finally {
      setLoading(false);
    }
  };

  const register = async (credentials: RegisterCredentials) => {
    setLoading(true);
    try {
      const response = await authService.register(credentials);
      console.log('Register response:', response); // Debug log

      // After successful registration, automatically log in
      // since most backends don't return a token on register
      try {
        const loginResponse = await authService.login({
          username: credentials.username,
          password: credentials.password,
        });
        const { user: parsedUser, token } = parseAuthResponse(loginResponse, credentials.username);

        setUser(parsedUser);
        localStorage.setItem('token', token);
        localStorage.setItem('user', JSON.stringify(parsedUser));
      } catch {
        // If auto-login fails, still use register response
        const { user: parsedUser, token } = parseAuthResponse(response, credentials.username);
        setUser(parsedUser);
        localStorage.setItem('token', token);
        localStorage.setItem('user', JSON.stringify(parsedUser));
      }
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  };

  const value: AuthContextType = {
    user,
    loading,
    login,
    register,
    logout,
    isAuthenticated: !!user,
  };

  return React.createElement(AuthContext.Provider, { value }, children);
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};