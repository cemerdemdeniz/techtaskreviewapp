import { type ReactNode } from 'react';
import { Card } from '../../../shared/components/ui/Card';

type TrendDirection = 'up' | 'down' | 'neutral';

interface StatsCardProps {
  title: string;
  value: string | number;
  icon?: ReactNode;
  trend?: {
    value: string;
    direction: TrendDirection;
  };
  className?: string;
}

const trendColors: Record<TrendDirection, string> = {
  up: 'text-green-600',
  down: 'text-red-600',
  neutral: 'text-gray-500',
};

const trendIcons: Record<TrendDirection, string> = {
  up: '\u2191',
  down: '\u2193',
  neutral: '\u2192',
};

export function StatsCard({ title, value, icon, trend, className = '' }: StatsCardProps) {
  return (
    <Card className={`flex flex-col gap-3 ${className}`} padding="md">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-gray-500">{title}</span>
        {icon && (
          <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-indigo-50 text-indigo-600">
            {icon}
          </span>
        )}
      </div>
      <div className="flex items-end gap-2">
        <span className="text-2xl font-bold text-gray-900">{value}</span>
        {trend && (
          <span className={`mb-0.5 flex items-center text-sm font-medium ${trendColors[trend.direction]}`}>
            <span className="mr-0.5">{trendIcons[trend.direction]}</span>
            {trend.value}
          </span>
        )}
      </div>
    </Card>
  );
}
