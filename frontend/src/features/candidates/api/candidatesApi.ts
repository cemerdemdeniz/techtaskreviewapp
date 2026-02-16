import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import type { ApiResponse, PaginatedResponse } from '@/lib/api/types';
import type {
  CandidateListItem,
  CandidateDetail,
  CreateCandidateRequest,
} from '../types';

const CANDIDATE_QUERY_KEYS = {
  all: ['candidates'] as const,
  list: (page: number, pageSize: number, role?: string, search?: string) =>
    ['candidates', 'list', { page, pageSize, role, search }] as const,
  detail: (id: string) => ['candidates', id] as const,
};

async function fetchCandidates(
  page: number,
  pageSize: number,
  role?: string,
  search?: string,
): Promise<PaginatedResponse<CandidateListItem>> {
  const params: Record<string, string | number> = { page, pageSize };
  if (role) params.role = role;
  if (search) params.search = search;

  const response = await apiClient.get<PaginatedResponse<CandidateListItem>>(
    '/candidates',
    { params },
  );
  return response.data;
}

async function fetchCandidate(id: string): Promise<CandidateDetail> {
  const response = await apiClient.get<ApiResponse<CandidateDetail>>(
    `/candidates/${id}`,
  );
  return response.data.data;
}

async function createCandidate(
  data: CreateCandidateRequest,
): Promise<CandidateDetail> {
  const response = await apiClient.post<ApiResponse<CandidateDetail>>(
    '/candidates',
    data,
  );
  return response.data.data;
}

export function useCandidates(
  page: number,
  pageSize: number,
  role?: string,
  search?: string,
) {
  return useQuery({
    queryKey: CANDIDATE_QUERY_KEYS.list(page, pageSize, role, search),
    queryFn: () => fetchCandidates(page, pageSize, role, search),
    placeholderData: (previousData) => previousData,
  });
}

export function useCandidate(id: string) {
  return useQuery({
    queryKey: CANDIDATE_QUERY_KEYS.detail(id),
    queryFn: () => fetchCandidate(id),
    enabled: !!id,
  });
}

export function useCreateCandidate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createCandidate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CANDIDATE_QUERY_KEYS.all });
    },
  });
}
