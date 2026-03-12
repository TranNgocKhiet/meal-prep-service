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

export const getErrorMessage = (error: unknown): string => {
  const err = error as ApiError;
  return err.response?.data?.message || err.message || 'An error occurred';
};
