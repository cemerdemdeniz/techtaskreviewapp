import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import type { ApiResponse } from '@/lib/api/types';
import type { ReviewDetail, ReviewComparison } from '../types';

const REVIEW_QUERY_KEYS = {
  all: ['reviews'] as const,
  detail: (id: string) => ['reviews', id] as const,
  bySubmission: (submissionId: string) =>
    ['reviews', 'by-submission', submissionId] as const,
  comparison: (candidateIds: string[]) =>
    ['reviews', 'comparison', ...candidateIds] as const,
  export: (id: string) => ['reviews', id, 'export'] as const,
};

async function fetchReview(id: string): Promise<ReviewDetail> {
  const response = await apiClient.get<ApiResponse<ReviewDetail>>(
    `/reviews/${id}`,
  );
  return response.data.data;
}

async function fetchReviewBySubmission(
  submissionId: string,
): Promise<ReviewDetail> {
  const response = await apiClient.get<ApiResponse<ReviewDetail>>(
    `/reviews/by-submission/${submissionId}`,
  );
  return response.data.data;
}

async function fetchReviewComparison(
  candidateIds: string[],
): Promise<ReviewComparison> {
  const params = new URLSearchParams();
  candidateIds.forEach((id) => params.append('candidateIds', id));
  const response = await apiClient.get<ApiResponse<ReviewComparison>>(
    `/reviews/compare?${params.toString()}`,
  );
  return response.data.data;
}

async function fetchReviewExportPdf(id: string): Promise<Blob> {
  const response = await apiClient.get(`/reviews/${id}/export`, {
    responseType: 'blob',
  });
  return response.data;
}

export function useReview(id: string) {
  return useQuery({
    queryKey: REVIEW_QUERY_KEYS.detail(id),
    queryFn: () => fetchReview(id),
    enabled: !!id,
  });
}

export function useReviewBySubmission(submissionId: string) {
  return useQuery({
    queryKey: REVIEW_QUERY_KEYS.bySubmission(submissionId),
    queryFn: () => fetchReviewBySubmission(submissionId),
    enabled: !!submissionId,
  });
}

export function useReviewComparison(candidateIds: string[]) {
  return useQuery({
    queryKey: REVIEW_QUERY_KEYS.comparison(candidateIds),
    queryFn: () => fetchReviewComparison(candidateIds),
    enabled: candidateIds.length >= 2,
  });
}

export function useExportReviewPdf(id: string) {
  return useQuery({
    queryKey: REVIEW_QUERY_KEYS.export(id),
    queryFn: () => fetchReviewExportPdf(id),
    enabled: false,
  });
}
