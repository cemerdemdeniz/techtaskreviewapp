import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import type { ApiResponse } from '@/lib/api/types';
import type {
  ScoringConfig,
  UpdateScoringConfigRequest,
} from '../types';

const SCORING_CONFIG_QUERY_KEYS = {
  all: ['scoring-configs'] as const,
};

async function fetchScoringConfigs(): Promise<ScoringConfig[]> {
  const response = await apiClient.get<ApiResponse<ScoringConfig[]>>(
    '/scoring-configs',
  );
  return response.data.data;
}

async function updateScoringConfig(params: {
  id: string;
  data: UpdateScoringConfigRequest;
}): Promise<ScoringConfig> {
  const response = await apiClient.put<ApiResponse<ScoringConfig>>(
    `/scoring-configs/${params.id}`,
    params.data,
  );
  return response.data.data;
}

export function useScoringConfigs() {
  return useQuery({
    queryKey: SCORING_CONFIG_QUERY_KEYS.all,
    queryFn: fetchScoringConfigs,
  });
}

export function useUpdateScoringConfig() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: updateScoringConfig,
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: SCORING_CONFIG_QUERY_KEYS.all,
      });
    },
  });
}
