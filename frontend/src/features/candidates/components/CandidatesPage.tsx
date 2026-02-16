import { useState, useMemo, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card } from '@/shared/components/ui/Card';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { Input } from '@/shared/components/ui/Input';
import { Spinner } from '@/shared/components/ui/Spinner';
import { ScoreBadge } from '@/features/reviews/components/ScoreBar';
import { useCandidates } from '../api/candidatesApi';
import { CreateCandidateModal } from './CreateCandidateModal';

const ROLE_TABS = ['All', 'Frontend', 'Backend'] as const;
const PAGE_SIZE = 10;

export function CandidatesPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [activeRole, setActiveRole] = useState<string>('All');
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);

  const roleFilter = activeRole === 'All' ? undefined : activeRole;
  const { data, isLoading } = useCandidates(
    page,
    PAGE_SIZE,
    roleFilter,
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

  const handleRoleChange = useCallback((role: string) => {
    setActiveRole(role);
    setPage(1);
  }, []);

  const roleBadgeVariant = useCallback(
    (role: string) => (role === 'Frontend' ? 'info' : 'success'),
    [],
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Candidates</h1>
          <p className="mt-1 text-sm text-gray-500">
            Manage candidates and view their submission history.
          </p>
        </div>
        <Button onClick={() => setIsModalOpen(true)}>Add Candidate</Button>
      </div>

      {/* Filters */}
      <Card>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          {/* Role Tabs */}
          <div className="flex rounded-lg border border-gray-200 p-1">
            {ROLE_TABS.map((role) => (
              <button
                key={role}
                onClick={() => handleRoleChange(role)}
                className={`rounded-md px-4 py-1.5 text-sm font-medium transition-colors ${
                  activeRole === role
                    ? 'bg-primary-600 text-white shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                {role}
              </button>
            ))}
          </div>

          {/* Search */}
          <div className="flex gap-2">
            <Input
              placeholder="Search by name or email..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={handleSearchKeyDown}
              className="w-64"
            />
            <Button variant="secondary" size="sm" onClick={handleSearch}>
              Search
            </Button>
          </div>
        </div>
      </Card>

      {/* Table */}
      <Card padding="none">
        {isLoading ? (
          <div className="flex justify-center py-12">
            <Spinner size="lg" />
          </div>
        ) : !data || data.data.length === 0 ? (
          <div className="py-12 text-center text-gray-500">
            No candidates found.
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 bg-gray-50">
                    <th className="px-6 py-3 text-left font-semibold text-gray-700">
                      Name
                    </th>
                    <th className="px-6 py-3 text-left font-semibold text-gray-700">
                      Email
                    </th>
                    <th className="px-6 py-3 text-center font-semibold text-gray-700">
                      Role
                    </th>
                    <th className="px-6 py-3 text-center font-semibold text-gray-700">
                      Submissions
                    </th>
                    <th className="px-6 py-3 text-center font-semibold text-gray-700">
                      Latest Score
                    </th>
                    <th className="px-6 py-3 text-right font-semibold text-gray-700">
                      Created
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {data.data.map((candidate) => (
                    <tr
                      key={candidate.id}
                      onClick={() => navigate(`/candidates/${candidate.id}`)}
                      className="cursor-pointer hover:bg-gray-50 transition-colors"
                    >
                      <td className="px-6 py-4 font-medium text-gray-900">
                        {candidate.fullName}
                      </td>
                      <td className="px-6 py-4 text-gray-600">
                        {candidate.email}
                      </td>
                      <td className="px-6 py-4 text-center">
                        <Badge variant={roleBadgeVariant(candidate.role)}>
                          {candidate.role}
                        </Badge>
                      </td>
                      <td className="px-6 py-4 text-center tabular-nums text-gray-700">
                        {candidate.submissionCount}
                      </td>
                      <td className="px-6 py-4 text-center">
                        {candidate.latestScore !== null ? (
                          <ScoreBadge score={candidate.latestScore} />
                        ) : (
                          <span className="text-gray-400">--</span>
                        )}
                      </td>
                      <td className="px-6 py-4 text-right text-gray-500">
                        {new Date(candidate.createdAt).toLocaleDateString()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {data.totalPages > 1 && (
              <div className="flex items-center justify-between border-t border-gray-200 px-6 py-3">
                <p className="text-sm text-gray-500">
                  Showing page {data.page} of {data.totalPages} ({data.total}{' '}
                  total)
                </p>
                <div className="flex gap-2">
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={page <= 1}
                    onClick={() => setPage((p) => p - 1)}
                  >
                    Previous
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={page >= data.totalPages}
                    onClick={() => setPage((p) => p + 1)}
                  >
                    Next
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </Card>

      <CreateCandidateModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
      />
    </div>
  );
}
