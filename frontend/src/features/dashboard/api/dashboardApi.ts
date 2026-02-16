import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import type { ApiResponse } from '@/lib/api/types';
import type { DashboardSummary } from '../types';

const DASHBOARD_QUERY_KEYS = {
  summary: ['dashboard', 'summary'] as const,
};

async function fetchDashboardSummary(): Promise<DashboardSummary> {
  const response = await apiClient.get<ApiResponse<DashboardSummary>>(
    '/dashboard/summary',
  );
  return response.data.data;
}

export function useDashboardSummary() {
  return useQuery({
    queryKey: DASHBOARD_QUERY_KEYS.summary,
    queryFn: fetchDashboardSummary,
    staleTime: 2 * 60 * 1000,
    refetchInterval: 5 * 60 * 1000,
  });
}
