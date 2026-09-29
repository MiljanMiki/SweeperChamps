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
  isAdmin: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: React.ReactNode;
}

// ── JWT decoder (payload je base64url) ──
function decodeJwt(token: string): Record<string, any> {
  try {
    const payload = token.split('.')[1];
    if (!payload) return {};
    // base64url → base64
    const b64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = b64.padEnd(b64.length + ((4 - (b64.length % 4)) % 4), '=');
    const json = decodeURIComponent(
      atob(padded)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(json);
  } catch {
    return {};
  }
}

// Role claim name emitted by ASP.NET Core JWT
const ROLE_CLAIM =
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const NAME_CLAIM =
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const NAMEID_CLAIM =
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
const EMAIL_CLAIM =
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';

function roleFromJwt(token: string): string {
  const decoded = decodeJwt(token);
  return (
    decoded[ROLE_CLAIM] ??
    decoded.role ??
    decoded.roles ??
    'User'
  );
}

function userFromJwt(token: string, fallbackUsername: string): User {
  const decoded = decodeJwt(token);
  return {
    id: String(decoded[NAMEID_CLAIM] ?? decoded.nameid ?? decoded.sub ?? fallbackUsername),
    username: String(decoded[NAME_CLAIM] ?? decoded.unique_name ?? fallbackUsername),
    email: String(decoded[EMAIL_CLAIM] ?? decoded.email ?? ''),
    role: roleFromJwt(token),
  };
}

// Helper to extract user + token from various response shapes
function parseAuthResponse(response: AuthResponse, fallbackUsername: string): { user: User; token: string } {
  // Case 1: { user: {...}, token: "..." }
  if (response.user && response.token) {
    // Uvek obogati user-a sa role iz JWT (jer backend user DTO ne mora da nosi role)
    const user = {
      ...response.user,
      role: response.user.role || roleFromJwt(response.token),
    };
    return { user, token: response.token };
  }

  // Case 2: Response IS the user, and token might be in a header we don't have
  if (response.username && response.token) {
    const user = userFromJwt(response.token, response.username);
    return { user, token: response.token };
  }

  // Case 3: Response is just a token string (already wrapped by api service)
  if (response.token && !response.user) {
    const user = userFromJwt(response.token, fallbackUsername);
    return { user, token: response.token };
  }

  // Fallback — nema tokena, ali imamo username
  const user: User = {
    id: fallbackUsername,
    username: fallbackUsername,
    email: '',
    role: 'User',
  };
  return { user, token: '' };
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('token');
    const storedUser = localStorage.getItem('user');

    if (token && storedUser) {
      try {
        const parsed = JSON.parse(storedUser);
        // Uvek re-izvuci role iz tokena (u slučaju da je stari user bez role)
        const role = roleFromJwt(token);
        setUser({ ...parsed, role });
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
      console.log('Login response:', response);

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
      console.log('Register response:', response);

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
        const { user: parsedUser, token } = parseAuthResponse(response, credentials.username);
        setUser(parsedUser);
        if (token) {
          localStorage.setItem('token', token);
          localStorage.setItem('user', JSON.stringify(parsedUser));
        }
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
    isAdmin: user?.role === 'Admin',
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