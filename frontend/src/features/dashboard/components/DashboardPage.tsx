import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
} from 'recharts';
import { Card, CardHeader } from '../../../shared/components/ui/Card';
import { Spinner } from '../../../shared/components/ui/Spinner';
import { Badge } from '../../../shared/components/ui/Badge';
import { StatsCard } from './StatsCard';
import { useDashboardSummary } from '../api/dashboardApi';
import type { TopCandidate } from '../types';

function formatDate(dateStr: string): string {
  const date = new Date(dateStr);
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

function formatDateTime(dateStr: string): string {
  const date = new Date(dateStr);
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function getScoreBadgeVariant(score: number) {
  if (score >= 80) return 'success' as const;
  if (score >= 60) return 'warning' as const;
  return 'error' as const;
}

function SubmissionsChart({ data }: { data: { date: string; count: number }[] }) {
  const formatted = data.map((d) => ({ ...d, date: formatDate(d.date) }));

  return (
    <Card>
      <CardHeader title="Submissions by Day" description="Daily submission volume over time" />
      <div className="h-72">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={formatted} margin={{ top: 8, right: 24, left: 0, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
            <XAxis
              dataKey="date"
              tick={{ fontSize: 12, fill: '#6b7280' }}
              tickLine={false}
              axisLine={{ stroke: '#e5e7eb' }}
            />
            <YAxis
              tick={{ fontSize: 12, fill: '#6b7280' }}
              tickLine={false}
              axisLine={false}
              allowDecimals={false}
            />
            <Tooltip
              contentStyle={{
                borderRadius: '8px',
                border: '1px solid #e5e7eb',
                boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)',
              }}
            />
            <Line
              type="monotone"
              dataKey="count"
              name="Submissions"
              stroke="#4f46e5"
              strokeWidth={2}
              dot={{ r: 3, fill: '#4f46e5' }}
              activeDot={{ r: 5, fill: '#4f46e5' }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </Card>
  );
}

function ScoreDistributionChart({ data }: { data: { range: string; count: number }[] }) {
  return (
    <Card>
      <CardHeader title="Score Distribution" description="Breakdown of review scores" />
      <div className="h-72">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 8, right: 24, left: 0, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" vertical={false} />
            <XAxis
              dataKey="range"
              tick={{ fontSize: 12, fill: '#6b7280' }}
              tickLine={false}
              axisLine={{ stroke: '#e5e7eb' }}
            />
            <YAxis
              tick={{ fontSize: 12, fill: '#6b7280' }}
              tickLine={false}
              axisLine={false}
              allowDecimals={false}
            />
            <Tooltip
              contentStyle={{
                borderRadius: '8px',
                border: '1px solid #e5e7eb',
                boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)',
              }}
            />
            <Bar dataKey="count" name="Candidates" fill="#818cf8" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </Card>
  );
}

function TopCandidatesTable({ candidates }: { candidates: TopCandidate[] }) {
  return (
    <Card padding="none">
      <div className="p-6 pb-0">
        <CardHeader title="Top Candidates" description="Highest scoring submissions" />
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-left">
          <thead>
            <tr className="border-b border-gray-200">
              <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                Candidate
              </th>
              <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                Role
              </th>
              <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                Score
              </th>
              <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                Submitted
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {candidates.map((candidate) => (
              <tr key={candidate.candidateId} className="hover:bg-gray-50 transition-colors">
                <td className="whitespace-nowrap px-6 py-4">
                  <span className="text-sm font-medium text-gray-900">
                    {candidate.fullName}
                  </span>
                </td>
                <td className="whitespace-nowrap px-6 py-4">
                  <span className="text-sm text-gray-600">{candidate.role}</span>
                </td>
                <td className="whitespace-nowrap px-6 py-4">
                  <Badge variant={getScoreBadgeVariant(candidate.score)} size="md">
                    {candidate.score}
                  </Badge>
                </td>
                <td className="whitespace-nowrap px-6 py-4">
                  <span className="text-sm text-gray-500">
                    {formatDateTime(candidate.submittedAt)}
                  </span>
                </td>
              </tr>
            ))}
            {candidates.length === 0 && (
              <tr>
                <td colSpan={4} className="px-6 py-10 text-center text-sm text-gray-400">
                  No candidates to display.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

export function DashboardPage() {
  const { data: summary, isLoading, isError, error } = useDashboardSummary();

  if (isLoading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <Spinner size="lg" className="text-indigo-600" />
          <p className="text-sm text-gray-500">Loading dashboard...</p>
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Card className="max-w-md text-center">
          <h3 className="text-lg font-semibold text-gray-900">Failed to load dashboard</h3>
          <p className="mt-2 text-sm text-gray-500">
            {error instanceof Error ? error.message : 'An unexpected error occurred.'}
          </p>
        </Card>
      </div>
    );
  }

  if (!summary) {
    return null;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="mt-1 text-sm text-gray-500">
          Overview of task submissions and review metrics.
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
        <StatsCard
          title="Total Submissions"
          value={summary.totalSubmissions.toLocaleString()}
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z" />
            </svg>
          }
        />
        <StatsCard
          title="Pending Reviews"
          value={summary.pendingReviews.toLocaleString()}
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
            </svg>
          }
        />
        <StatsCard
          title="Completed"
          value={summary.completedReviews.toLocaleString()}
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
            </svg>
          }
        />
        <StatsCard
          title="Failed"
          value={summary.failedReviews.toLocaleString()}
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="m9.75 9.75 4.5 4.5m0-4.5-4.5 4.5M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
            </svg>
          }
        />
        <StatsCard
          title="Avg Score"
          value={summary.averageScore.toFixed(1)}
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M11.48 3.499a.562.562 0 0 1 1.04 0l2.125 5.111a.563.563 0 0 0 .475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 0 0-.182.557l1.285 5.385a.562.562 0 0 1-.84.61l-4.725-2.885a.562.562 0 0 0-.586 0L6.982 20.54a.562.562 0 0 1-.84-.61l1.285-5.386a.562.562 0 0 0-.182-.557l-4.204-3.602a.562.562 0 0 1 .321-.988l5.518-.442a.563.563 0 0 0 .475-.345L11.48 3.5Z" />
            </svg>
          }
        />
        <StatsCard
          title="Avg Time (min)"
          value={summary.averageProcessingTimeMinutes.toFixed(1)}
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75Z" />
            </svg>
          }
        />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <SubmissionsChart data={summary.submissionsByDay} />
        <ScoreDistributionChart data={summary.scoreDistribution} />
      </div>

      {/* Top Candidates Table */}
      <TopCandidatesTable candidates={summary.topCandidates} />
    </div>
  );
}
