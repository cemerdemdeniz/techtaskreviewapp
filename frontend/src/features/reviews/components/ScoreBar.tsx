interface ScoreBarProps {
  score: number;
  maxScore?: number;
  showLabel?: boolean;
  height?: 'sm' | 'md' | 'lg';
  className?: string;
}

const heightClasses = {
  sm: 'h-2',
  md: 'h-3',
  lg: 'h-4',
};

function getScoreColor(score: number, maxScore: number): string {
  const ratio = score / maxScore;
  if (ratio >= 0.8) return 'bg-green-500';
  if (ratio >= 0.6) return 'bg-lime-500';
  if (ratio >= 0.4) return 'bg-yellow-500';
  if (ratio >= 0.2) return 'bg-orange-500';
  return 'bg-red-500';
}

function getScoreGradient(score: number, maxScore: number): string {
  const ratio = score / maxScore;
  if (ratio >= 0.8) return 'from-green-400 to-green-600';
  if (ratio >= 0.6) return 'from-lime-400 to-lime-600';
  if (ratio >= 0.4) return 'from-yellow-400 to-yellow-600';
  if (ratio >= 0.2) return 'from-orange-400 to-orange-600';
  return 'from-red-400 to-red-600';
}

export function ScoreBar({
  score,
  maxScore = 10,
  showLabel = true,
  height = 'md',
  className = '',
}: ScoreBarProps) {
  const percentage = Math.min((score / maxScore) * 100, 100);

  return (
    <div className={`flex items-center gap-3 ${className}`}>
      <div
        className={`relative flex-1 overflow-hidden rounded-full bg-gray-200 ${heightClasses[height]}`}
      >
        <div
          className={`absolute inset-y-0 left-0 rounded-full bg-gradient-to-r ${getScoreGradient(score, maxScore)} transition-all duration-500 ease-out`}
          style={{ width: `${percentage}%` }}
        />
      </div>
      {showLabel && (
        <span
          className={`text-sm font-semibold tabular-nums ${getScoreColor(score, maxScore).replace('bg-', 'text-')}`}
        >
          {score.toFixed(1)}
        </span>
      )}
    </div>
  );
}

export function ScoreBadge({
  score,
  maxScore = 10,
}: {
  score: number;
  maxScore?: number;
}) {
  const color = getScoreColor(score, maxScore);

  return (
    <span
      className={`inline-flex items-center justify-center rounded-full ${color} px-2.5 py-0.5 text-xs font-bold text-white`}
    >
      {score.toFixed(1)}
    </span>
  );
}
