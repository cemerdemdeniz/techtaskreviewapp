import { useState, useEffect, useMemo } from 'react';
import {
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  Tooltip,
  Legend,
} from 'recharts';
import { Card, CardHeader } from '@/shared/components/ui/Card';
import { Button } from '@/shared/components/ui/Button';
import { Input } from '@/shared/components/ui/Input';
import { Spinner } from '@/shared/components/ui/Spinner';
import {
  useScoringConfigs,
  useUpdateScoringConfig,
} from '../api/scoringConfigApi';
import type { ScoringConfig, CategoryWeightItem } from '../types';

const ROLE_TABS = ['Frontend', 'Backend'] as const;

const PIE_COLORS = [
  '#6366f1',
  '#f43f5e',
  '#10b981',
  '#f59e0b',
  '#8b5cf6',
  '#06b6d4',
  '#ec4899',
  '#14b8a6',
];

interface EditableWeight {
  category: string;
  categoryDisplayName: string;
  weight: number;
  minPassingScore: number;
}

function WeightSlider({
  item,
  onChange,
}: {
  item: EditableWeight;
  onChange: (category: string, field: 'weight' | 'minPassingScore', value: number) => void;
}) {
  return (
    <div className="rounded-lg border border-gray-200 p-4">
      <div className="flex items-center justify-between mb-3">
        <h4 className="text-sm font-semibold text-gray-900">
          {item.categoryDisplayName}
        </h4>
        <span className="text-sm font-bold text-primary-600 tabular-nums">
          {item.weight}%
        </span>
      </div>

      {/* Weight Slider */}
      <div className="mb-4">
        <label className="mb-1 block text-xs font-medium text-gray-500">
          Weight
        </label>
        <input
          type="range"
          min={0}
          max={100}
          value={item.weight}
          onChange={(e) =>
            onChange(item.category, 'weight', Number(e.target.value))
          }
          className="w-full h-2 rounded-full appearance-none bg-gray-200 accent-primary-600 cursor-pointer"
        />
        <div className="flex justify-between text-xs text-gray-400 mt-0.5">
          <span>0%</span>
          <span>100%</span>
        </div>
      </div>

      {/* Min Passing Score */}
      <div>
        <Input
          label="Min Passing Score"
          type="number"
          min={0}
          max={10}
          step={0.5}
          value={item.minPassingScore}
          onChange={(e) =>
            onChange(
              item.category,
              'minPassingScore',
              Number(e.target.value),
            )
          }
        />
      </div>
    </div>
  );
}

export function ScoringConfigPage() {
  const { data: configs, isLoading } = useScoringConfigs();
  const updateMutation = useUpdateScoringConfig();

  const [activeRole, setActiveRole] = useState<string>('Frontend');
  const [editableWeights, setEditableWeights] = useState<EditableWeight[]>([]);
  const [isDirty, setIsDirty] = useState(false);

  const activeConfig = useMemo(
    () => configs?.find((c) => c.role === activeRole),
    [configs, activeRole],
  );

  useEffect(() => {
    if (activeConfig) {
      setEditableWeights(
        activeConfig.weights.map((w) => ({
          category: w.category,
          categoryDisplayName: w.categoryDisplayName,
          weight: w.weight,
          minPassingScore: w.minPassingScore,
        })),
      );
      setIsDirty(false);
    }
  }, [activeConfig]);

  const handleWeightChange = (
    category: string,
    field: 'weight' | 'minPassingScore',
    value: number,
  ) => {
    setEditableWeights((prev) =>
      prev.map((w) =>
        w.category === category ? { ...w, [field]: value } : w,
      ),
    );
    setIsDirty(true);
  };

  const totalWeight = useMemo(
    () => editableWeights.reduce((sum, w) => sum + w.weight, 0),
    [editableWeights],
  );

  const handleSave = async () => {
    if (!activeConfig) return;

    await updateMutation.mutateAsync({
      id: activeConfig.id,
      data: {
        weights: editableWeights.map((w) => ({
          category: w.category,
          weight: w.weight,
          minPassingScore: w.minPassingScore,
        })),
      },
    });
    setIsDirty(false);
  };

  const pieData = editableWeights
    .filter((w) => w.weight > 0)
    .map((w) => ({
      name: w.categoryDisplayName,
      value: w.weight,
    }));

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Scoring Configuration
          </h1>
          <p className="mt-1 text-sm text-gray-500">
            Configure category weights and minimum passing scores per role.
          </p>
        </div>
        <Button
          onClick={handleSave}
          disabled={!isDirty || totalWeight !== 100}
          loading={updateMutation.isPending}
        >
          Save Changes
        </Button>
      </div>

      {/* Role Tabs */}
      <div className="flex rounded-lg border border-gray-200 p-1 w-fit">
        {ROLE_TABS.map((role) => (
          <button
            key={role}
            onClick={() => setActiveRole(role)}
            className={`rounded-md px-6 py-2 text-sm font-medium transition-colors ${
              activeRole === role
                ? 'bg-primary-600 text-white shadow-sm'
                : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            {role}
          </button>
        ))}
      </div>

      {/* Weight Warning */}
      {totalWeight !== 100 && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3">
          <p className="text-sm text-amber-800">
            Total weight is currently{' '}
            <span className="font-bold">{totalWeight}%</span>. It must equal
            100% before saving.
          </p>
        </div>
      )}

      {updateMutation.isError && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3">
          <p className="text-sm text-red-800">
            Failed to save configuration. Please try again.
          </p>
        </div>
      )}

      {updateMutation.isSuccess && !isDirty && (
        <div className="rounded-lg border border-green-200 bg-green-50 px-4 py-3">
          <p className="text-sm text-green-800">
            Configuration saved successfully.
          </p>
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Weight Sliders */}
        <div className="lg:col-span-2 space-y-4">
          {editableWeights.map((item) => (
            <WeightSlider
              key={item.category}
              item={item}
              onChange={handleWeightChange}
            />
          ))}
        </div>

        {/* Pie Chart */}
        <div>
          <Card className="sticky top-6">
            <CardHeader title="Weight Distribution" />
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={100}
                  paddingAngle={2}
                  dataKey="value"
                  label={({ name, value }) => `${name}: ${value}%`}
                  labelLine={false}
                >
                  {pieData.map((_, idx) => (
                    <Cell
                      key={idx}
                      fill={PIE_COLORS[idx % PIE_COLORS.length]}
                    />
                  ))}
                </Pie>
                <Tooltip
                  formatter={(value: number) => `${value}%`}
                />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
            <div className="mt-4 rounded-lg bg-gray-50 p-3 text-center">
              <p className="text-sm text-gray-500">Total Weight</p>
              <p
                className={`text-2xl font-bold ${
                  totalWeight === 100 ? 'text-green-600' : 'text-amber-600'
                }`}
              >
                {totalWeight}%
              </p>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
