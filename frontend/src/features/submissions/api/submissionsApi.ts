import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import type { ApiResponse, PaginatedResponse } from '@/lib/api/types';
import type {
  SubmissionListItem,
  SubmissionDetail,
  SubmissionStatus,
  CreateGitSubmissionRequest,
} from '../types';

const SUBMISSIONS_KEY = 'submissions';

export function useSubmissions(
  page: number,
  pageSize: number,
  status?: SubmissionStatus,
  search?: string,
) {
  return useQuery({
    queryKey: [SUBMISSIONS_KEY, page, pageSize, status, search],
    queryFn: async () => {
      const params: Record<string, string | number> = { page, pageSize };
      if (status) params.status = status;
      if (search) params.search = search;

      const response = await apiClient.get<PaginatedResponse<SubmissionListItem>>(
        '/submissions',
        { params },
      );
      return response.data;
    },
  });
}

export function useSubmission(id: string) {
  return useQuery({
    queryKey: [SUBMISSIONS_KEY, id],
    queryFn: async () => {
      const response = await apiClient.get<ApiResponse<SubmissionDetail>>(
        `/submissions/${id}`,
      );
      return response.data.data;
    },
    enabled: !!id,
  });
}

export function useSubmissionStatus(id: string) {
  return useQuery({
    queryKey: [SUBMISSIONS_KEY, id, 'status'],
    queryFn: async () => {
      const response = await apiClient.get<ApiResponse<{ status: SubmissionStatus }>>(
        `/submissions/${id}/status`,
      );
      return response.data.data;
    },
    enabled: !!id,
    refetchInterval: 5000,
  });
}

export function useCreateSubmission() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ candidateId, file }: { candidateId: string; file: File }) => {
      const formData = new FormData();
      formData.append('candidateId', candidateId);
      formData.append('file', file);

      const response = await apiClient.post<ApiResponse<SubmissionDetail>>(
        '/submissions',
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } },
      );
      return response.data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [SUBMISSIONS_KEY] });
    },
  });
}

export function useCreateGitSubmission() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: CreateGitSubmissionRequest) => {
      const response = await apiClient.post<ApiResponse<SubmissionDetail>>(
        '/submissions/git',
        data,
      );
      return response.data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [SUBMISSIONS_KEY] });
    },
  });
}

export function useRetrySubmission() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      const response = await apiClient.post<ApiResponse<SubmissionDetail>>(
        `/submissions/${id}/retry`,
      );
      return response.data.data;
    },
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: [SUBMISSIONS_KEY] });
      queryClient.invalidateQueries({ queryKey: [SUBMISSIONS_KEY, id] });
    },
  });
}
