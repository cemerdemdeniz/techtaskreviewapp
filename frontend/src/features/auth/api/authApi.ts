import { apiClient } from '@/lib/api/client';
import type { LoginRequest, LoginResponse, User } from '@/features/auth/types';
import type { ApiResponse } from '@/lib/api/types';

export const authApi = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<ApiResponse<LoginResponse>>(
      '/auth/login',
      data,
    );
    return response.data.data;
  },

  getProfile: async (): Promise<User> => {
    const response = await apiClient.get<ApiResponse<User>>('/auth/profile');
    return response.data.data;
  },

  refreshToken: async (): Promise<{ token: string }> => {
    const response = await apiClient.post<ApiResponse<{ token: string }>>(
      '/auth/refresh',
    );
    return response.data.data;
  },
};
