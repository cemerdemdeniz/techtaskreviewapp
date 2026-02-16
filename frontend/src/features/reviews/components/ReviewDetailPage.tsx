import { useParams } from 'react-router-dom';
import {
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
  ResponsiveContainer,
  Tooltip,
} from 'recharts';
import { Card, CardHeader } from '@/shared/components/ui/Card';
import { Badge } from '@/shared/components/ui/Badge';
import { Button } from '@/shared/components/ui/Button';
import { Spinner } from '@/shared/components/ui/Spinner';
import { useReview, useExportReviewPdf } from '../api/reviewsApi';
import { ScoreBar } from './ScoreBar';
import type { CategoryScoreItem } from '../types';

function ScoreGauge({ score, label }: { score: number; label: string }) {
  const radius = 70;
  const circumference = Math.PI * radius;
  const percentage = (score / 10) * 100;
  const offset = circumference - (percentage / 100) * circumference;

  const getColor = (s: number) => {
    if (s >= 8) return '#22c55e';
    if (s >= 6) return '#84cc16';
    if (s >= 4) return '#eab308';
    if (s >= 2) return '#f97316';
    return '#ef4444';
  };

  return (
    <div className="flex flex-col items-center">
      <svg width="160" height="100" viewBox="0 0 160 100">
        <path
          d="M 10 90 A 70 70 0 0 1 150 90"
          fill="none"
          stroke="#e5e7eb"
          strokeWidth="12"
          strokeLinecap="round"
        />
        <path
          d="M 10 90 A 70 70 0 0 1 150 90"
          fill="none"
          stroke={getColor(score)}
          strokeWidth="12"
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          className="transition-all duration-700 ease-out"
        />
        <text
          x="80"
          y="80"
          textAnchor="middle"
          className="text-3xl font-bold"
          fill={getColor(score)}
        >
          {score.toFixed(1)}
        </text>
      </svg>
      <span className="mt-1 text-sm font-medium text-gray-500">{label}</span>
    </div>
  );
}

function CategoryScoreCard({ category }: { category: CategoryScoreItem }) {
  return (
    <Card>
      <div className="flex items-start justify-between mb-3">
        <h4 className="text-sm font-semibold text-gray-900">
          {category.categoryDisplayName}
        </h4>
        <Badge
          variant={category.confidence >= 0.8 ? 'success' : 'warning'}
          size="sm"
        >
          {(category.confidence * 100).toFixed(0)}% conf.
        </Badge>
      </div>

      <ScoreBar score={category.score} className="mb-3" />

      <p className="text-sm text-gray-600 mb-3">{category.explanation}</p>

      {category.suggestions.length > 0 && (
        <div>
          <p className="text-xs font-medium text-gray-500 uppercase tracking-wide mb-1.5">
            Suggestions
          </p>
          <ul className="space-y-1">
            {category.suggestions.map((suggestion, i) => (
              <li
                key={i}
                className="flex items-start gap-2 text-sm text-gray-600"
              >
                <span className="mt-1 h-1.5 w-1.5 flex-shrink-0 rounded-full bg-primary-400" />
                {suggestion}
              </li>
            ))}
          </ul>
        </div>
      )}
    </Card>
  );
}

export function ReviewDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: review, isLoading, error } = useReview(id!);
  const { refetch: exportPdf, isFetching: isExporting } =
    useExportReviewPdf(id!);

  const handleExportPdf = async () => {
    const result = await exportPdf();
    if (result.data) {
      const url = URL.createObjectURL(result.data);
      const a = document.createElement('a');
      a.href = url;
      a.download = `review-${id}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error || !review) {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-red-600">Failed to load review details.</p>
      </div>
    );
  }

  const radarData = review.categoryScores.map((cs) => ({
    category: cs.categoryDisplayName,
    score: cs.score,
    fullMark: 10,
  }));

  const statusVariant =
    review.status === 'Completed'
      ? 'success'
      : review.status === 'Failed'
        ? 'error'
        : 'warning';

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Review: {review.candidateFullName}
          </h1>
          <p className="mt-1 text-sm text-gray-500">
            {review.candidateRole} candidate
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Badge variant={statusVariant} size="md">
            {review.status}
          </Badge>
          <Button
            variant="secondary"
            size="sm"
            loading={isExporting}
            onClick={handleExportPdf}
          >
            Export PDF
          </Button>
        </div>
      </div>

      {/* Score Overview + Radar */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-1">
          <CardHeader title="Score Overview" />
          <div className="flex flex-col items-center gap-6">
            <ScoreGauge score={review.overallScore} label="Overall Score" />
            <div className="grid w-full grid-cols-2 gap-4 text-center">
              <div>
                <p className="text-2xl font-bold text-gray-900">
                  {(review.confidence * 100).toFixed(0)}%
                </p>
                <p className="text-xs text-gray-500">Confidence</p>
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">
                  {review.processingTimeSeconds.toFixed(1)}s
                </p>
                <p className="text-xs text-gray-500">Processing Time</p>
              </div>
            </div>
          </div>
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader title="Category Radar" />
          <ResponsiveContainer width="100%" height={320}>
            <RadarChart data={radarData}>
              <PolarGrid stroke="#e5e7eb" />
              <PolarAngleAxis
                dataKey="category"
                tick={{ fontSize: 12, fill: '#6b7280' }}
              />
              <PolarRadiusAxis
                angle={90}
                domain={[0, 10]}
                tick={{ fontSize: 10, fill: '#9ca3af' }}
              />
              <Radar
                name="Score"
                dataKey="score"
                stroke="#6366f1"
                fill="#6366f1"
                fillOpacity={0.2}
                strokeWidth={2}
              />
              <Tooltip />
            </RadarChart>
          </ResponsiveContainer>
        </Card>
      </div>

      {/* Processing Info */}
      <Card>
        <div className="flex flex-wrap gap-6 text-sm">
          <div>
            <span className="font-medium text-gray-500">Version:</span>{' '}
            <span className="text-gray-900">{review.version}</span>
          </div>
          <div>
            <span className="font-medium text-gray-500">Created:</span>{' '}
            <span className="text-gray-900">
              {new Date(review.createdAt).toLocaleString()}
            </span>
          </div>
          {review.completedAt && (
            <div>
              <span className="font-medium text-gray-500">Completed:</span>{' '}
              <span className="text-gray-900">
                {new Date(review.completedAt).toLocaleString()}
              </span>
            </div>
          )}
          <div>
            <span className="font-medium text-gray-500">Submission:</span>{' '}
            <span className="font-mono text-gray-900">
              {review.submissionId}
            </span>
          </div>
        </div>
      </Card>

      {/* Category Scores Grid */}
      <div>
        <h2 className="mb-4 text-lg font-semibold text-gray-900">
          Category Scores
        </h2>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          {review.categoryScores.map((cs) => (
            <CategoryScoreCard key={cs.category} category={cs} />
          ))}
        </div>
      </div>
    </div>
  );
}
