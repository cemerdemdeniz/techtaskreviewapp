import { useMutation, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { authApi } from '@/features/auth/api/authApi';
import { useAuthStore } from '@/features/auth/store/authStore';
import type { LoginRequest } from '@/features/auth/types';

export function useLogin() {
  const { setAuth } = useAuthStore();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: (response) => {
      setAuth(response.user, response.token);
      navigate('/dashboard');
    },
  });
}

export function useProfile() {
  const { isAuthenticated } = useAuthStore();

  return useQuery({
    queryKey: ['auth', 'profile'],
    queryFn: authApi.getProfile,
    enabled: isAuthenticated,
  });
}

export function useLogout() {
  const { logout } = useAuthStore();
  const navigate = useNavigate();

  return () => {
    logout();
    navigate('/login');
  };
}
