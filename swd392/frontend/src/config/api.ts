import axios from 'axios';
import { notifyGlobalError, sanitizeErrorMessage } from '../types/errors';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5013/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const requestUrl = (error.config?.url || '').toString();
    const isAuthEndpoint =
      requestUrl.includes('/auth/login') ||
      requestUrl.includes('/auth/register') ||
      requestUrl.includes('/auth/google-login') ||
      requestUrl.includes('/auth/google-register');

    if (error.response?.status === 401 && !isAuthEndpoint) {
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }

    const responseMessage = error.response?.data?.message;
    const fallbackMessage =
      responseMessage || error.message || 'An unexpected error occurred. Please try again.';
    const sanitizedMessage = sanitizeErrorMessage(fallbackMessage);

    if (error.response?.data && typeof error.response.data === 'object') {
      error.response.data.message = sanitizedMessage;
    }
    error.message = sanitizedMessage;

    const containsLocalhostDetails =
      typeof fallbackMessage === 'string' &&
      /localhost|127\.0\.0\.1|network error|failed to fetch|econnrefused|timeout/i.test(fallbackMessage);

    if (containsLocalhostDetails) {
      notifyGlobalError(sanitizedMessage);
    }

    return Promise.reject(error);
  }
);

export default apiClient;
export { API_BASE_URL };
