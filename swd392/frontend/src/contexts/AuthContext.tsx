import { createContext, useState, useEffect, type ReactNode } from 'react';
import apiClient from '../config/api';

interface User {
  id: string;
  email: string;
  fullName: string;
  phoneNumber: string;
  roleName: string;
  currentCredits?: number;
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, fullName: string) => Promise<void>;
  loginWithGoogle: (googleToken: string) => Promise<void>;
  registerWithGoogle: (googleToken: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider = ({ children }: AuthProviderProps) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const initAuth = async () => {
      const token = localStorage.getItem('authToken');
      if (token) {
        try {
          await refreshUser();
        } catch (error) {
          console.error('Failed to refresh user:', error);
          localStorage.removeItem('authToken');
          localStorage.removeItem('refreshToken');
        }
      }
      setIsLoading(false);
    };

    initAuth();
  }, []);

  const refreshUser = async () => {
    try {
      const response = await apiClient.get('/auth/me');
      if (response.data.success) {
        setUser(response.data.data);
      }
    } catch (error) {
      console.error('Failed to fetch user:', error);
      throw error;
    }
  };

  const login = async (email: string, password: string) => {
    try {
      const response = await apiClient.post('/auth/login', { email, password });
      
      if (response.data.success) {
        const { token, refreshToken, user: userData } = response.data.data;
        localStorage.setItem('authToken', token);
        localStorage.setItem('refreshToken', refreshToken);
        setUser(userData);
      } else {
        throw new Error(response.data.message || 'Login failed');
      }
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } }; message?: string };
      const message = err.response?.data?.message || err.message || 'Login failed';
      throw new Error(message);
    }
  };

  const register = async (email: string, password: string, fullName: string) => {
    try {
      const response = await apiClient.post('/auth/register', {
        email,
        password,
        fullName,
        phoneNumber: '',
        roleName: 'Customer'
      });
      
      if (response.data.success) {
        const { token, refreshToken, user: userData } = response.data.data;
        localStorage.setItem('authToken', token);
        localStorage.setItem('refreshToken', refreshToken);
        setUser(userData);
      } else {
        throw new Error(response.data.message || 'Registration failed');
      }
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } }; message?: string };
      const message = err.response?.data?.message || err.message || 'Registration failed';
      throw new Error(message);
    }
  };

  const loginWithGoogle = async (googleToken: string) => {
    try {
      const response = await apiClient.post('/auth/google-login', { googleToken });
      
      if (response.data.success) {
        const { token, refreshToken, user: userData } = response.data.data;
        localStorage.setItem('authToken', token);
        localStorage.setItem('refreshToken', refreshToken);
        setUser(userData);
      } else {
        throw new Error(response.data.message || 'Google login failed');
      }
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } }; message?: string };
      const message = err.response?.data?.message || err.message || 'Google login failed';
      throw new Error(message);
    }
  };

  const registerWithGoogle = async (googleToken: string) => {
    try {
      const response = await apiClient.post('/auth/google-register', { googleToken });

      if (response.data.success) {
        const { token, refreshToken, user: userData } = response.data.data;
        localStorage.setItem('authToken', token);
        localStorage.setItem('refreshToken', refreshToken);
        setUser(userData);
      } else {
        throw new Error(response.data.message || 'Google signup failed');
      }
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } }; message?: string };
      const message = err.response?.data?.message || err.message || 'Google signup failed';
      throw new Error(message);
    }
  };

  const logout = async () => {
    try {
      await apiClient.post('/auth/logout');
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
      setUser(null);
    }
  };

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    register,
    loginWithGoogle,
    registerWithGoogle,
    logout,
    refreshUser
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
