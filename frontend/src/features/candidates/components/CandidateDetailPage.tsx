import { useParams, useNavigate, Link } from 'react-router-dom';
import { Card, CardHeader } from '@/shared/components/ui/Card';
import { Badge } from '@/shared/components/ui/Badge';
import { Button } from '@/shared/components/ui/Button';
import { Spinner } from '@/shared/components/ui/Spinner';
import { ScoreBadge } from '@/features/reviews/components/ScoreBar';
import { useCandidate } from '../api/candidatesApi';

function getStatusVariant(
  status: string,
): 'success' | 'warning' | 'error' | 'info' | 'default' {
  switch (status) {
    case 'Completed':
      return 'success';
    case 'Processing':
    case 'Pending':
      return 'warning';
    case 'Failed':
      return 'error';
    default:
      return 'default';
  }
}

export function CandidateDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: candidate, isLoading, error } = useCandidate(id!);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !candidate) {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-red-600">Failed to load candidate details.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <button
            onClick={() => navigate('/candidates')}
            className="mb-2 text-sm text-gray-500 hover:text-gray-700 transition-colors"
          >
            &larr; Back to Candidates
          </button>
          <h1 className="text-2xl font-bold text-gray-900">
            {candidate.fullName}
          </h1>
        </div>
        <Link to={`/compare?candidateIds=${candidate.id}`}>
          <Button variant="secondary">Compare with Others</Button>
        </Link>
      </div>

      {/* Info Card */}
      <Card>
        <CardHeader title="Candidate Information" />
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <p className="text-sm font-medium text-gray-500">Email</p>
            <p className="mt-1 text-gray-900">{candidate.email}</p>
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Role</p>
            <div className="mt-1">
              <Badge
                variant={candidate.role === 'Frontend' ? 'info' : 'success'}
                size="md"
              >
                {candidate.role}
              </Badge>
            </div>
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Submissions</p>
            <p className="mt-1 text-2xl font-bold text-gray-900">
              {candidate.submissionCount}
            </p>
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Latest Score</p>
            <div className="mt-1">
              {candidate.latestScore !== null ? (
                <ScoreBadge score={candidate.latestScore} />
              ) : (
                <span className="text-gray-400">No reviews yet</span>
              )}
            </div>
          </div>
        </div>
      </Card>

      {/* Submissions History */}
      <Card padding="none">
        <div className="p-6 pb-0">
          <CardHeader title="Submissions History" />
        </div>
        {candidate.submissions.length === 0 ? (
          <div className="py-8 text-center text-gray-500">
            No submissions yet.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 bg-gray-50">
                  <th className="px-6 py-3 text-left font-semibold text-gray-700">
                    Submission ID
                  </th>
                  <th className="px-6 py-3 text-center font-semibold text-gray-700">
                    Status
                  </th>
                  <th className="px-6 py-3 text-center font-semibold text-gray-700">
                    Score
                  </th>
                  <th className="px-6 py-3 text-right font-semibold text-gray-700">
                    Submitted
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {candidate.submissions.map((sub) => (
                  <tr
                    key={sub.id}
                    className="cursor-pointer hover:bg-gray-50 transition-colors"
                    onClick={() => navigate(`/reviews/by-submission/${sub.id}`)}
                  >
                    <td className="px-6 py-4 font-mono text-sm text-gray-700">
                      {sub.id.slice(0, 8)}...
                    </td>
                    <td className="px-6 py-4 text-center">
                      <Badge variant={getStatusVariant(sub.status)}>
                        {sub.status}
                      </Badge>
                    </td>
                    <td className="px-6 py-4 text-center">
                      {sub.overallScore !== null ? (
                        <ScoreBadge score={sub.overallScore} />
                      ) : (
                        <span className="text-gray-400">--</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-right text-gray-500">
                      {new Date(sub.createdAt).toLocaleDateString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}
