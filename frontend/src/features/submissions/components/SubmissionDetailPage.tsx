import { useParams, useNavigate, Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button';
import { Card, CardHeader } from '@/shared/components/ui/Card';
import { Badge } from '@/shared/components/ui/Badge';
import { Spinner } from '@/shared/components/ui/Spinner';
import { useSubmission, useSubmissionStatus, useRetrySubmission } from '../api/submissionsApi';
import type { SubmissionStatus } from '../types';

const PIPELINE_STEPS: SubmissionStatus[] = [
  'Uploaded',
  'Extracting',
  'Scanning',
  'Analyzing',
  'Reviewing',
  'Scoring',
  'Completed',
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

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

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

function getStepState(
  currentStatus: SubmissionStatus,
  step: SubmissionStatus,
): 'completed' | 'current' | 'pending' {
  const currentIndex = PIPELINE_STEPS.indexOf(currentStatus);
  const stepIndex = PIPELINE_STEPS.indexOf(step);

  if (currentIndex < 0) return 'pending';
  if (stepIndex < currentIndex) return 'completed';
  if (stepIndex === currentIndex) return 'current';
  return 'pending';
}

export function SubmissionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: submission, isLoading, isError } = useSubmission(id!);
  const { data: statusData } = useSubmissionStatus(id!);
  const retryMutation = useRetrySubmission();

  const currentStatus = statusData?.status ?? submission?.status;
  const isProcessing =
    currentStatus &&
    currentStatus !== 'Completed' &&
    currentStatus !== 'Failed' &&
    currentStatus !== 'PartiallyCompleted';

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Spinner size="lg" />
      </div>
    );
  }

  if (isError || !submission) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-gray-500">
        <p className="text-lg font-medium">Submission not found</p>
        <Button variant="secondary" className="mt-4" onClick={() => navigate('/submissions')}>
          Back to Submissions
        </Button>
      </div>
    );
  }

  const handleRetry = () => {
    retryMutation.mutate(submission.id);
  };

  return (
    <div className="space-y-6">
      {/* Back + Header */}
      <div>
        <button
          onClick={() => navigate('/submissions')}
          className="mb-3 inline-flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 transition-colors"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Back to Submissions
        </button>
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              {submission.candidateFullName}
            </h1>
            <p className="mt-1 text-sm text-gray-500">{submission.candidateRole}</p>
          </div>
          <div className="flex items-center gap-3">
            {currentStatus && (
              <Badge variant={statusBadgeVariant[currentStatus]} size="md">
                {currentStatus}
              </Badge>
            )}
            {(currentStatus === 'Failed' || currentStatus === 'PartiallyCompleted') && (
              <Button
                variant="danger"
                size="sm"
                loading={retryMutation.isPending}
                onClick={handleRetry}
              >
                Retry
              </Button>
            )}
            {currentStatus === 'Completed' && (
              <Link to={`/submissions/${submission.id}/review`}>
                <Button size="sm">View Review</Button>
              </Link>
            )}
          </div>
        </div>
      </div>

      {/* Status Progress Banner */}
      {currentStatus && currentStatus !== 'Failed' && (
        <Card>
          <CardHeader title="Processing Pipeline" />
          <div className="flex items-center gap-0">
            {PIPELINE_STEPS.map((step, index) => {
              const state = getStepState(currentStatus, step);
              return (
                <div key={step} className="flex flex-1 items-center">
                  <div className="flex flex-col items-center flex-1">
                    <div
                      className={`flex h-8 w-8 items-center justify-center rounded-full text-xs font-semibold transition-colors ${
                        state === 'completed'
                          ? 'bg-green-500 text-white'
                          : state === 'current'
                            ? 'bg-primary-600 text-white ring-4 ring-primary-100'
                            : 'bg-gray-200 text-gray-500'
                      }`}
                    >
                      {state === 'completed' ? (
                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                        </svg>
                      ) : state === 'current' && isProcessing ? (
                        <Spinner size="sm" />
                      ) : (
                        index + 1
                      )}
                    </div>
                    <span
                      className={`mt-1.5 text-xs font-medium ${
                        state === 'completed'
                          ? 'text-green-600'
                          : state === 'current'
                            ? 'text-primary-600'
                            : 'text-gray-400'
                      }`}
                    >
                      {step}
                    </span>
                  </div>
                  {index < PIPELINE_STEPS.length - 1 && (
                    <div
                      className={`h-0.5 w-full ${
                        state === 'completed' ? 'bg-green-500' : 'bg-gray-200'
                      }`}
                    />
                  )}
                </div>
              );
            })}
          </div>
        </Card>
      )}

      {/* Error Banner */}
      {(currentStatus === 'Failed' || currentStatus === 'PartiallyCompleted') &&
        submission.errorMessage && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4">
            <div className="flex items-start gap-3">
              <svg
                className="h-5 w-5 text-red-500 mt-0.5 flex-shrink-0"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <div>
                <h4 className="text-sm font-semibold text-red-800">Processing Error</h4>
                <p className="mt-1 text-sm text-red-700">{submission.errorMessage}</p>
              </div>
            </div>
          </div>
        )}

      {/* Info Cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <p className="text-xs font-medium uppercase tracking-wider text-gray-500">Source</p>
          <p className="mt-1 text-lg font-semibold text-gray-900">{submission.source}</p>
          {submission.repositoryUrl && (
            <a
              href={submission.repositoryUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="mt-1 inline-block text-xs text-primary-600 hover:underline truncate max-w-full"
            >
              {submission.repositoryUrl}
            </a>
          )}
        </Card>
        <Card>
          <p className="text-xs font-medium uppercase tracking-wider text-gray-500">
            Project Type
          </p>
          <p className="mt-1 text-lg font-semibold text-gray-900">{submission.projectType}</p>
        </Card>
        <Card>
          <p className="text-xs font-medium uppercase tracking-wider text-gray-500">Files</p>
          <p className="mt-1 text-lg font-semibold text-gray-900">{submission.fileCount}</p>
        </Card>
        <Card>
          <p className="text-xs font-medium uppercase tracking-wider text-gray-500">Total Size</p>
          <p className="mt-1 text-lg font-semibold text-gray-900">
            {formatBytes(submission.totalSizeBytes)}
          </p>
        </Card>
      </div>

      {/* Submission Details */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Card>
          <p className="text-xs font-medium uppercase tracking-wider text-gray-500">Submitted</p>
          <p className="mt-1 text-sm font-medium text-gray-900">
            {formatDate(submission.createdAt)}
          </p>
        </Card>
        <Card>
          <p className="text-xs font-medium uppercase tracking-wider text-gray-500">Completed</p>
          <p className="mt-1 text-sm font-medium text-gray-900">
            {formatDate(submission.completedAt)}
          </p>
        </Card>
      </div>

      {/* Score */}
      {submission.overallScore !== null && (
        <Card>
          <CardHeader title="Overall Score" />
          <div className="flex items-center gap-4">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-primary-50">
              <span className="text-2xl font-bold text-primary-700">
                {submission.overallScore}
              </span>
            </div>
            <div className="flex-1">
              <div className="h-3 w-full rounded-full bg-gray-200">
                <div
                  className="h-3 rounded-full bg-primary-600 transition-all"
                  style={{ width: `${submission.overallScore}%` }}
                />
              </div>
            </div>
            <span className="text-sm font-medium text-gray-500">/100</span>
          </div>
        </Card>
      )}

      {/* File List */}
      {submission.files && submission.files.length > 0 && (
        <Card padding="none">
          <div className="px-6 pt-6 pb-3">
            <CardHeader
              title="Files"
              description={`${submission.files.length} files in submission`}
            />
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="border-y border-gray-200 bg-gray-50">
                <tr>
                  <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600">
                    Path
                  </th>
                  <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600">
                    Language
                  </th>
                  <th className="whitespace-nowrap px-6 py-3 font-medium text-gray-600 text-right">
                    Size
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {submission.files.map((file) => (
                  <tr key={file.id} className="hover:bg-gray-50">
                    <td className="px-6 py-3 font-mono text-xs text-gray-900">
                      {file.relativePath}
                    </td>
                    <td className="whitespace-nowrap px-6 py-3">
                      <Badge variant="default">{file.language}</Badge>
                    </td>
                    <td className="whitespace-nowrap px-6 py-3 text-right text-gray-500">
                      {formatBytes(file.sizeBytes)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </div>
  );
}
