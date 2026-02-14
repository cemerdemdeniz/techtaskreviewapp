# 08 — Frontend Architecture

## Project Structure

```
frontend/
├── public/
│   ├── favicon.ico
│   └── index.html
├── src/
│   ├── app/
│   │   ├── App.tsx                    # Root component, providers, router
│   │   ├── routes.tsx                 # Route definitions
│   │   └── providers.tsx              # Composed context providers
│   │
│   ├── features/                      # Feature-based modules
│   │   ├── auth/
│   │   │   ├── components/
│   │   │   │   ├── LoginForm.tsx
│   │   │   │   └── ProtectedRoute.tsx
│   │   │   ├── hooks/
│   │   │   │   └── useAuth.ts
│   │   │   ├── api/
│   │   │   │   └── authApi.ts
│   │   │   ├── store/
│   │   │   │   └── authStore.ts       # Zustand slice
│   │   │   └── types.ts
│   │   │
│   │   ├── submissions/
│   │   │   ├── components/
│   │   │   │   ├── SubmissionList.tsx
│   │   │   │   ├── SubmissionCard.tsx
│   │   │   │   ├── SubmissionDetail.tsx
│   │   │   │   ├── UploadForm.tsx
│   │   │   │   ├── GitUrlForm.tsx
│   │   │   │   ├── SubmissionStatusBadge.tsx
│   │   │   │   └── ProcessingProgress.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── useSubmissions.ts
│   │   │   │   ├── useSubmission.ts
│   │   │   │   └── useSubmissionStatus.ts  # Polling hook
│   │   │   ├── api/
│   │   │   │   └── submissionsApi.ts
│   │   │   └── types.ts
│   │   │
│   │   ├── reviews/
│   │   │   ├── components/
│   │   │   │   ├── ReviewDetail.tsx
│   │   │   │   ├── ScoreRadarChart.tsx
│   │   │   │   ├── CategoryScoreCard.tsx
│   │   │   │   ├── CategoryScoreList.tsx
│   │   │   │   ├── CriticalIssuesList.tsx
│   │   │   │   ├── ImprovementPlan.tsx
│   │   │   │   └── ReviewSummary.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── useReview.ts
│   │   │   │   └── useExportPdf.ts
│   │   │   ├── api/
│   │   │   │   └── reviewsApi.ts
│   │   │   └── types.ts
│   │   │
│   │   ├── candidates/
│   │   │   ├── components/
│   │   │   │   ├── CandidateList.tsx
│   │   │   │   ├── CandidateCard.tsx
│   │   │   │   ├── CandidateDetail.tsx
│   │   │   │   ├── CandidateForm.tsx
│   │   │   │   └── CandidateCompare.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── useCandidates.ts
│   │   │   │   └── useCandidate.ts
│   │   │   ├── api/
│   │   │   │   └── candidatesApi.ts
│   │   │   └── types.ts
│   │   │
│   │   ├── dashboard/
│   │   │   ├── components/
│   │   │   │   ├── DashboardPage.tsx
│   │   │   │   ├── MetricCard.tsx
│   │   │   │   ├── ScoreDistributionChart.tsx
│   │   │   │   ├── RecentSubmissionsTable.tsx
│   │   │   │   ├── TopCandidatesTable.tsx
│   │   │   │   └── ProcessingQueueStatus.tsx
│   │   │   ├── hooks/
│   │   │   │   └── useDashboard.ts
│   │   │   ├── api/
│   │   │   │   └── dashboardApi.ts
│   │   │   └── types.ts
│   │   │
│   │   ├── comparison/
│   │   │   ├── components/
│   │   │   │   ├── ComparisonPage.tsx
│   │   │   │   ├── ComparisonTable.tsx
│   │   │   │   ├── ComparisonRadar.tsx
│   │   │   │   └── CandidateSelector.tsx
│   │   │   ├── hooks/
│   │   │   │   └── useComparison.ts
│   │   │   └── api/
│   │   │       └── comparisonApi.ts
│   │   │
│   │   ├── scoring-config/
│   │   │   ├── components/
│   │   │   │   ├── ScoringConfigList.tsx
│   │   │   │   ├── ScoringConfigEditor.tsx
│   │   │   │   └── WeightSlider.tsx
│   │   │   ├── hooks/
│   │   │   │   └── useScoringConfig.ts
│   │   │   └── api/
│   │   │       └── scoringConfigApi.ts
│   │   │
│   │   └── admin/
│   │       ├── components/
│   │       │   ├── AdminDashboard.tsx
│   │       │   ├── UserManagement.tsx
│   │       │   ├── SystemHealth.tsx
│   │       │   └── AuditLogViewer.tsx
│   │       ├── hooks/
│   │       │   └── useAdmin.ts
│   │       └── api/
│   │           └── adminApi.ts
│   │
│   ├── shared/
│   │   ├── components/
│   │   │   ├── ui/                    # Reusable UI primitives
│   │   │   │   ├── Button.tsx
│   │   │   │   ├── Input.tsx
│   │   │   │   ├── Select.tsx
│   │   │   │   ├── Modal.tsx
│   │   │   │   ├── Table.tsx
│   │   │   │   ├── Badge.tsx
│   │   │   │   ├── Card.tsx
│   │   │   │   ├── Skeleton.tsx       # Loading placeholder
│   │   │   │   ├── Toast.tsx
│   │   │   │   └── Spinner.tsx
│   │   │   ├── layout/
│   │   │   │   ├── AppLayout.tsx
│   │   │   │   ├── Sidebar.tsx
│   │   │   │   ├── Header.tsx
│   │   │   │   └── PageContainer.tsx
│   │   │   └── ErrorBoundary.tsx
│   │   ├── hooks/
│   │   │   ├── useDebounce.ts
│   │   │   ├── usePagination.ts
│   │   │   └── useLocalStorage.ts
│   │   └── utils/
│   │       ├── formatters.ts          # Date, number, score formatting
│   │       ├── validators.ts
│   │       └── constants.ts
│   │
│   ├── lib/
│   │   ├── api/
│   │   │   ├── client.ts              # Axios instance with interceptors
│   │   │   └── types.ts               # ApiResponse<T>, PaginatedResponse<T>
│   │   └── queryClient.ts             # TanStack Query client config
│   │
│   ├── styles/
│   │   └── globals.css                # Tailwind base + custom utilities
│   │
│   └── main.tsx                       # Entry point
│
├── package.json
├── tsconfig.json
├── tailwind.config.ts
├── vite.config.ts
└── .eslintrc.cjs
```

## State Architecture

### Layer Separation

```
┌───────────────────────────────────────────────────────────┐
│                    Component Layer                         │
│  React components consume hooks, render UI                │
└───────────────────────────┬───────────────────────────────┘
                            │ uses
┌───────────────────────────▼───────────────────────────────┐
│                    Hook Layer                              │
│  Custom hooks compose TanStack Query + Zustand            │
│  Single source of truth for data access                   │
└──────────┬────────────────────────────────┬───────────────┘
           │ server state                    │ client state
┌──────────▼──────────┐          ┌──────────▼──────────────┐
│  TanStack Query     │          │  Zustand Store          │
│  (React Query)      │          │                         │
│  - Server data      │          │  - UI state (modals,    │
│  - Caching          │          │    sidebar, filters)    │
│  - Background       │          │  - Auth state           │
│    refetch          │          │  - User preferences     │
│  - Optimistic       │          │                         │
│    updates          │          │                         │
└──────────┬──────────┘          └─────────────────────────┘
           │
┌──────────▼──────────┐
│  API Layer          │
│  (Axios client)     │
│  - Request/response │
│  - Auth headers     │
│  - Error transform  │
└─────────────────────┘
```

### API Client

```typescript
// lib/api/client.ts
import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/features/auth/store/authStore';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 30_000,
  headers: { 'Content-Type': 'application/json' },
});

// Auth interceptor
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Error interceptor
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiError>) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout();
      window.location.href = '/login';
    }
    return Promise.reject(transformError(error));
  }
);

export { apiClient };

// lib/api/types.ts
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
```

### TanStack Query Configuration

```typescript
// lib/queryClient.ts
import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,          // 30s before refetch
      gcTime: 5 * 60_000,         // 5min garbage collection
      retry: 2,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: 0,
    },
  },
});
```

### Zustand Store

```typescript
// features/auth/store/authStore.ts
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  accessToken: string | null;
  user: User | null;
  isAuthenticated: boolean;
  login: (token: string, user: User) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      user: null,
      isAuthenticated: false,
      login: (accessToken, user) =>
        set({ accessToken, user, isAuthenticated: true }),
      logout: () =>
        set({ accessToken: null, user: null, isAuthenticated: false }),
    }),
    { name: 'auth-storage' }
  )
);
```

### Feature Hook Example

```typescript
// features/submissions/hooks/useSubmissions.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { submissionsApi } from '../api/submissionsApi';
import type { SubmissionListDto, CreateSubmissionRequest } from '../types';

export function useSubmissions(page: number, pageSize: number = 20) {
  return useQuery({
    queryKey: ['submissions', { page, pageSize }],
    queryFn: () => submissionsApi.getAll(page, pageSize),
  });
}

export function useCreateSubmission() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: FormData) => submissionsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['submissions'] });
    },
  });
}

// features/submissions/hooks/useSubmissionStatus.ts
// Polls submission status during processing
export function useSubmissionStatus(submissionId: string, enabled: boolean) {
  return useQuery({
    queryKey: ['submission-status', submissionId],
    queryFn: () => submissionsApi.getStatus(submissionId),
    enabled,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      // Stop polling when terminal state reached
      if (status === 'Completed' || status === 'Failed' || status === 'Quarantined') {
        return false;
      }
      return 3_000; // Poll every 3 seconds
    },
  });
}
```

### Feature API Layer

```typescript
// features/submissions/api/submissionsApi.ts
import { apiClient } from '@/lib/api/client';
import type { ApiResponse, PaginatedResponse } from '@/lib/api/types';
import type { SubmissionListDto, SubmissionDetailDto, SubmissionStatusDto } from '../types';

export const submissionsApi = {
  getAll: async (page: number, pageSize: number) => {
    const { data } = await apiClient.get<PaginatedResponse<SubmissionListDto>>(
      '/submissions',
      { params: { page, pageSize } }
    );
    return data;
  },

  getById: async (id: string) => {
    const { data } = await apiClient.get<ApiResponse<SubmissionDetailDto>>(
      `/submissions/${id}`
    );
    return data.data;
  },

  getStatus: async (id: string) => {
    const { data } = await apiClient.get<ApiResponse<SubmissionStatusDto>>(
      `/submissions/${id}/status`
    );
    return data.data;
  },

  create: async (formData: FormData) => {
    const { data } = await apiClient.post<ApiResponse<{ id: string }>>(
      '/submissions',
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' }, timeout: 120_000 }
    );
    return data.data;
  },

  createFromGit: async (request: { candidateId: string; gitUrl: string; branch?: string }) => {
    const { data } = await apiClient.post<ApiResponse<{ id: string }>>(
      '/submissions/git',
      request
    );
    return data.data;
  },
};
```

## Key Component Designs

### Score Radar Chart

```typescript
// features/reviews/components/ScoreRadarChart.tsx
import { RadarChart, PolarGrid, PolarAngleAxis, PolarRadiusAxis, Radar, Tooltip } from 'recharts';

interface ScoreRadarChartProps {
  scores: CategoryScoreDto[];
  comparisonScores?: CategoryScoreDto[];  // For candidate comparison
  width?: number;
  height?: number;
}

export function ScoreRadarChart({
  scores,
  comparisonScores,
  width = 500,
  height = 400,
}: ScoreRadarChartProps) {
  const data = scores.map((s) => ({
    category: formatCategoryName(s.category),
    score: s.score,
    fullMark: 10,
    ...(comparisonScores
      ? { comparisonScore: comparisonScores.find(c => c.category === s.category)?.score ?? 0 }
      : {}),
  }));

  return (
    <RadarChart width={width} height={height} data={data}>
      <PolarGrid />
      <PolarAngleAxis dataKey="category" tick={{ fontSize: 11 }} />
      <PolarRadiusAxis angle={30} domain={[0, 10]} />
      <Radar
        name="Candidate"
        dataKey="score"
        stroke="#3b82f6"
        fill="#3b82f6"
        fillOpacity={0.3}
      />
      {comparisonScores && (
        <Radar
          name="Comparison"
          dataKey="comparisonScore"
          stroke="#ef4444"
          fill="#ef4444"
          fillOpacity={0.15}
        />
      )}
      <Tooltip />
    </RadarChart>
  );
}
```

### Processing Progress (Polling UI)

```typescript
// features/submissions/components/ProcessingProgress.tsx
import { useSubmissionStatus } from '../hooks/useSubmissionStatus';

const STAGES = [
  { key: 'Uploaded', label: 'Uploaded' },
  { key: 'Extracting', label: 'Extracting files' },
  { key: 'MalwareScanning', label: 'Security scan' },
  { key: 'StaticAnalysis', label: 'Static analysis' },
  { key: 'AIReview', label: 'AI code review' },
  { key: 'Scoring', label: 'Computing scores' },
  { key: 'Completed', label: 'Complete' },
] as const;

export function ProcessingProgress({ submissionId }: { submissionId: string }) {
  const { data: status, isLoading } = useSubmissionStatus(
    submissionId,
    true // enabled
  );

  if (isLoading || !status) return <Skeleton className="h-16 w-full" />;

  if (status.status === 'Failed') {
    return (
      <div className="rounded-md bg-red-50 p-4 border border-red-200">
        <p className="text-red-800 font-medium">Processing failed</p>
        <p className="text-red-600 text-sm mt-1">{status.failureReason}</p>
      </div>
    );
  }

  const currentIndex = STAGES.findIndex(s => s.key === status.status);

  return (
    <div className="space-y-3">
      {STAGES.map((stage, idx) => (
        <div key={stage.key} className="flex items-center gap-3">
          <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm
            ${idx < currentIndex ? 'bg-green-500 text-white' :
              idx === currentIndex ? 'bg-blue-500 text-white animate-pulse' :
              'bg-gray-200 text-gray-400'}`}>
            {idx < currentIndex ? '\u2713' : idx + 1}
          </div>
          <span className={idx <= currentIndex ? 'text-gray-900' : 'text-gray-400'}>
            {stage.label}
          </span>
        </div>
      ))}
    </div>
  );
}
```

## Error Handling UX

```typescript
// shared/components/ErrorBoundary.tsx
import { Component, type ReactNode } from 'react';

interface Props { children: ReactNode; fallback?: ReactNode; }
interface State { hasError: boolean; error?: Error; }

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback ?? (
        <div className="p-8 text-center">
          <h2 className="text-lg font-semibold text-gray-900">Something went wrong</h2>
          <p className="text-gray-500 mt-2">{this.state.error?.message}</p>
          <button
            onClick={() => this.setState({ hasError: false })}
            className="mt-4 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
          >
            Try again
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}
```

## Loading States Strategy

| Context | Pattern | Component |
|---------|---------|-----------|
| Page load | Full skeleton | `<Skeleton />` matching final layout shape |
| Table data | Skeleton rows | 5 placeholder rows with pulsing bars |
| Chart data | Empty chart frame | Chart axes rendered, data area pulsing |
| File upload | Progress bar | Deterministic progress from XHR events |
| Processing | Stage progress | `<ProcessingProgress />` with polling |
| PDF export | Spinner + message | `<Spinner />` with "Generating PDF..." |

## Large Result Rendering

Review detail pages can have 14+ category scores, each with arrays of issues and suggestions. Optimization:

1. **Virtualized lists**: Use `@tanstack/react-virtual` for category score lists if >20 items
2. **Lazy accordion**: Category details (justification, issues, suggestions) render only when expanded
3. **Memoization**: `React.memo` on `CategoryScoreCard` — scores don't change after load
4. **Code splitting**: Review detail page lazy-loaded via `React.lazy()` (it's the heaviest page)

```typescript
// routes.tsx
const ReviewDetail = lazy(() => import('@/features/reviews/components/ReviewDetail'));
const ComparisonPage = lazy(() => import('@/features/comparison/components/ComparisonPage'));

export const routes = [
  // ...
  { path: '/reviews/:id', element: <Suspense fallback={<PageSkeleton />}><ReviewDetail /></Suspense> },
  { path: '/compare', element: <Suspense fallback={<PageSkeleton />}><ComparisonPage /></Suspense> },
];
```

## Page Map

```
/login                          → LoginForm
/dashboard                      → DashboardPage (default after login)
/candidates                     → CandidateList
/candidates/new                 → CandidateForm
/candidates/:id                 → CandidateDetail
/submissions                    → SubmissionList
/submissions/new                → UploadForm / GitUrlForm (tabbed)
/submissions/:id                → SubmissionDetail + ProcessingProgress
/reviews/:id                    → ReviewDetail + ScoreRadarChart + CategoryScores
/compare                        → ComparisonPage (select 2 candidates)
/scoring-config                 → ScoringConfigList
/scoring-config/:id/edit        → ScoringConfigEditor
/admin                          → AdminDashboard
/admin/users                    → UserManagement
/admin/audit-log                → AuditLogViewer
/admin/health                   → SystemHealth
```
