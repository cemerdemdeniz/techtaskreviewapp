import { useState, useMemo } from 'react';
import {
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
  ResponsiveContainer,
  Tooltip,
  Legend,
} from 'recharts';
import { Card, CardHeader } from '@/shared/components/ui/Card';
import { Button } from '@/shared/components/ui/Button';
import { Spinner } from '@/shared/components/ui/Spinner';
import { useCandidates } from '@/features/candidates/api/candidatesApi';
import { useReviewComparison } from '../api/reviewsApi';

const CHART_COLORS = [
  '#6366f1',
  '#f43f5e',
  '#10b981',
  '#f59e0b',
  '#8b5cf6',
  '#06b6d4',
];

function getScoreCellColor(
  score: number,
  scores: number[],
): string {
  const max = Math.max(...scores);
  const min = Math.min(...scores);

  if (scores.length < 2 || max === min) return '';
  if (score === max) return 'bg-green-100 text-green-800';
  if (score === min) return 'bg-red-100 text-red-800';
  return 'bg-yellow-50 text-yellow-800';
}

export function ComparisonPage() {
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const { data: candidatesData, isLoading: loadingCandidates } = useCandidates(
    1,
    100,
  );
  const {
    data: comparison,
    isLoading: loadingComparison,
  } = useReviewComparison(selectedIds);

  const toggleCandidate = (id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((cid) => cid !== id) : [...prev, id],
    );
  };

  const radarData = useMemo(() => {
    if (!comparison) return [];
    return comparison.categories.map((cat) => {
      const point: Record<string, string | number> = { category: cat };
      comparison.candidates.forEach((c) => {
        point[c.fullName] = c.scores[cat] ?? 0;
      });
      return point;
    });
  }, [comparison]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          Candidate Comparison
        </h1>
        <p className="mt-1 text-sm text-gray-500">
          Select two or more candidates to compare their review scores
          side-by-side.
        </p>
      </div>

      {/* Candidate Picker */}
      <Card>
        <CardHeader
          title="Select Candidates"
          description="Choose candidates to include in the comparison"
        />
        {loadingCandidates ? (
          <div className="flex justify-center py-4">
            <Spinner />
          </div>
        ) : (
          <div className="flex flex-wrap gap-2">
            {candidatesData?.data.map((candidate) => {
              const isSelected = selectedIds.includes(candidate.id);
              return (
                <button
                  key={candidate.id}
                  onClick={() => toggleCandidate(candidate.id)}
                  className={`rounded-full px-4 py-2 text-sm font-medium transition-colors ${
                    isSelected
                      ? 'bg-primary-600 text-white shadow-sm'
                      : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                  }`}
                >
                  {candidate.fullName}
                  <span className="ml-1.5 text-xs opacity-70">
                    ({candidate.role})
                  </span>
                </button>
              );
            })}
          </div>
        )}
        {selectedIds.length > 0 && selectedIds.length < 2 && (
          <p className="mt-3 text-sm text-amber-600">
            Select at least one more candidate to enable comparison.
          </p>
        )}
      </Card>

      {loadingComparison && selectedIds.length >= 2 && (
        <div className="flex justify-center py-10">
          <Spinner size="lg" />
        </div>
      )}

      {comparison && (
        <>
          {/* Radar Chart Overlay */}
          <Card>
            <CardHeader title="Score Radar Overlay" />
            <ResponsiveContainer width="100%" height={400}>
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
                {comparison.candidates.map((candidate, idx) => (
                  <Radar
                    key={candidate.candidateId}
                    name={candidate.fullName}
                    dataKey={candidate.fullName}
                    stroke={CHART_COLORS[idx % CHART_COLORS.length]}
                    fill={CHART_COLORS[idx % CHART_COLORS.length]}
                    fillOpacity={0.1}
                    strokeWidth={2}
                  />
                ))}
                <Legend />
                <Tooltip />
              </RadarChart>
            </ResponsiveContainer>
          </Card>

          {/* Score Comparison Table */}
          <Card padding="none">
            <div className="p-6 pb-0">
              <CardHeader title="Score Comparison Table" />
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 bg-gray-50">
                    <th className="px-6 py-3 text-left font-semibold text-gray-700">
                      Category
                    </th>
                    {comparison.candidates.map((c) => (
                      <th
                        key={c.candidateId}
                        className="px-6 py-3 text-center font-semibold text-gray-700"
                      >
                        {c.fullName}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {/* Overall Score Row */}
                  <tr className="bg-gray-50 font-semibold">
                    <td className="px-6 py-3 text-gray-900">Overall Score</td>
                    {comparison.candidates.map((c) => {
                      const allOverall = comparison.candidates.map(
                        (x) => x.overallScore,
                      );
                      const cellColor = getScoreCellColor(
                        c.overallScore,
                        allOverall,
                      );
                      return (
                        <td
                          key={c.candidateId}
                          className={`px-6 py-3 text-center ${cellColor}`}
                        >
                          {c.overallScore.toFixed(1)}
                        </td>
                      );
                    })}
                  </tr>
                  {/* Category Rows */}
                  {comparison.categories.map((cat) => {
                    const scores = comparison.candidates.map(
                      (c) => c.scores[cat] ?? 0,
                    );
                    return (
                      <tr key={cat} className="hover:bg-gray-50">
                        <td className="px-6 py-3 text-gray-700">{cat}</td>
                        {comparison.candidates.map((c) => {
                          const score = c.scores[cat] ?? 0;
                          const cellColor = getScoreCellColor(score, scores);
                          return (
                            <td
                              key={c.candidateId}
                              className={`px-6 py-3 text-center tabular-nums ${cellColor}`}
                            >
                              {score.toFixed(1)}
                            </td>
                          );
                        })}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </Card>
        </>
      )}

      {!comparison && selectedIds.length < 2 && (
        <Card className="text-center py-12">
          <p className="text-gray-500">
            Select at least two candidates above to see their comparison.
          </p>
        </Card>
      )}
    </div>
  );
}
