import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button';
import { Card } from '@/shared/components/ui/Card';
import { Badge } from '@/shared/components/ui/Badge';
import { Input } from '@/shared/components/ui/Input';
import { Spinner } from '@/shared/components/ui/Spinner';
import { SkeletonTable } from '@/shared/components/ui/Skeleton';
import { useSubmissions } from '../api/submissionsApi';
import { CreateSubmissionModal } from './CreateSubmissionModal';
import type { SubmissionStatus, SubmissionListItem } from '../types';

const STATUS_OPTIONS: { label: string; value: SubmissionStatus | '' }[] = [
  { label: 'All Statuses', value: '' },
  { label: 'Uploaded', value: 'Uploaded' },
  { label: 'Extracting', value: 'Extracting' },
  { label: 'Scanning', value: 'Scanning' },
  { label: 'Analyzing', value: 'Analyzing' },
  { label: 'Reviewing', value: 'Reviewing' },
  { label: 'Scoring', value: 'Scoring' },
  { label: 'Completed', value: 'Completed' },
  { label: 'Failed', value: 'Failed' },
  { label: 'Partially Completed', value: 'PartiallyCompleted' },
];

const statusBadgeVariant: Record<SubmissionStatus, 'default' | 'success' | 'warning' | 'error' | 'info'> = {
  Uploaded: 'default',
  Extracting: 'info',
  Scanning: 'info',
  Analyzing: 'info',
  Reviewing: 'info',
  Scoring: 'info',
  Completed: 'success',
  Failed: 'error',
  PartiallyCompleted: 'warning',
};

function formatDate(dateStr: string | null): string {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function SubmissionsPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [statusFilter, setStatusFilter] = useState<SubmissionStatus | ''>('');
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  const { data, isLoading, isError, error } = useSubmissions(
    page,
    pageSize,
    statusFilter || undefined,
    search || undefined,
  );

  const handleSearch = useCallback(() => {
    setSearch(searchInput);
    setPage(1);
  }, [searchInput]);

  const handleSearchKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter') handleSearch();
    },
    [handleSearch],
  );

  const handleStatusChange = useCallback((e: React.ChangeEvent<HTMLSelectElement>) => {
    setStatusFilter(e.target.value as SubmissionStatus | '');
    setPage(1);
  }, []);

  const handleRowClick = useCallback(
    (submission: SubmissionListItem) => {
      navigate(`/submissions/${submission.id}`);
    },
    [navigate],
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Submissions</h1>
          <p className="mt-1 text-sm text-gray-500">
            Manage and review candidate task submissions
          </p>
        </div>
        <Button onClick={() => setIsCreateModalOpen(true)}>
          <svg className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          New Submission
        </Button>
      </div>

      {/* Filter Bar */}
      <Card padding="sm">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="w-full sm:w-48">
            <label
              htmlFor="status-filter"
              className="mb-1.5 block text-sm font-medium text-gray-700"
            >
              Status
            </label>
            <select
              id="status-filter"
              value={statusFilter}
              onChange={handleStatusChange}
              className="input-field w-full"
            >
              {STATUS_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
          <div className="flex-1">
            <Input
              label="Search"
              placeholder="Search by candidate name or role..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={handleSearchKeyDown}
            />
          </div>
          <Button variant="secondary" onClick={handleSearch}>
            Search
          </Button>
        </div>
      </Card>

      {/* Table */}
      <Card padding="none">
        {isLoading ? (
          <div className="p-6">
            <SkeletonTable rows={5} />
          </div>
        ) : isError ? (
          <div className="p-6 text-center text-red-600">
            <p>Failed to load submissions.</p>
            <p className="text-sm text-gray-500 mt-1">
              {(error as Error)?.message || 'An unexpected error occurred.'}
            </p>
          </div>
        ) : data && data.data.length > 0 ? (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-gray-200 bg-gray-50">
                  <tr>
                    <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600">
                      Candidate
                    </th>
                    <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600">
                      Role
                    </th>
                    <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600">
                      Status
                    </th>
                    <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600">
                      Source
                    </th>
                    <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600">
                      Score
                    </th>
                    <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600">
                      Submitted
                    </th>
                    <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600">
                      Completed
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {data.data.map((submission) => (
                    <tr
                      key={submission.id}
                      onClick={() => handleRowClick(submission)}
                      className="cursor-pointer hover:bg-gray-50 transition-colors"
                    >
                      <td className="whitespace-nowrap px-6 py-4 font-medium text-gray-900">
                        {submission.candidateFullName}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-gray-600">
                        {submission.candidateRole}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4">
                        <Badge variant={statusBadgeVariant[submission.status]}>
                          {submission.status}
                        </Badge>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-gray-600">
                        {submission.source}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-gray-600">
                        {submission.overallScore !== null ? (
                          <span className="font-semibold">{submission.overallScore}/100</span>
                        ) : (
                          <span className="text-gray-400">-</span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-gray-500">
                        {formatDate(submission.createdAt)}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-gray-500">
                        {formatDate(submission.completedAt)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            <div className="flex items-center justify-between border-t border-gray-200 px-6 py-3">
              <p className="text-sm text-gray-600">
                Showing page {data.page} of {data.totalPages} ({data.total} total)
              </p>
              <div className="flex items-center gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Previous
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={page >= (data.totalPages ?? 1)}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          </>
        ) : (
          <div className="flex flex-col items-center justify-center py-16 text-gray-500">
            <svg
              className="h-12 w-12 text-gray-300 mb-3"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={1.5}
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
            <p className="text-sm font-medium">No submissions found</p>
            <p className="text-xs mt-1">Create a new submission to get started.</p>
          </div>
        )}
      </Card>

      {/* Create Modal */}
      <CreateSubmissionModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
      />
    </div>
  );
}
