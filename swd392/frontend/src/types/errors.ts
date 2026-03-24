export interface ApiError {
  response?: {
    data?: {
      message?: string;
      errors?: Array<{ field: string; message: string }>;
    };
    status?: number;
  };
  message?: string;
}

export const APP_ERROR_EVENT = 'app:error';

const LOCALHOST_URL_PATTERN = /https?:\/\/(?:localhost|127\.0\.0\.1)(?::\d+)?[^\s)]*/gi;
const LOCALHOST_HOST_PATTERN = /(?:localhost|127\.0\.0\.1)(?::\d+)?/gi;

export const sanitizeErrorMessage = (
  message: string | undefined,
  fallback = 'An error occurred'
): string => {
  if (!message || !message.trim()) {
    return fallback;
  }

  const normalizedMessage = message.trim();
  const lowerMessage = normalizedMessage.toLowerCase();

  const isConnectionIssue =
    lowerMessage.includes('network error') ||
    lowerMessage.includes('failed to fetch') ||
    lowerMessage.includes('econnrefused') ||
    lowerMessage.includes('err_connection_refused') ||
    lowerMessage.includes('timeout');

  if (isConnectionIssue) {
    return 'Unable to connect to the server. Please try again later.';
  }

  const sanitized = normalizedMessage
    .replace(LOCALHOST_URL_PATTERN, 'the server')
    .replace(LOCALHOST_HOST_PATTERN, 'the server')
    .replace(/\s{2,}/g, ' ')
    .trim();

  return sanitized || fallback;
};

export const getErrorMessage = (error: unknown, fallback = 'An error occurred'): string => {
  const err = error as ApiError;
  const rawMessage = err.response?.data?.message || err.message;
  return sanitizeErrorMessage(rawMessage, fallback);
};

export const notifyGlobalError = (message: string): void => {
  if (typeof window === 'undefined') {
    return;
  }

  window.dispatchEvent(
    new CustomEvent(APP_ERROR_EVENT, {
      detail: { message },
    })
  );
};
