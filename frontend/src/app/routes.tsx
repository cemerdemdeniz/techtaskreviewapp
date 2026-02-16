import { Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from '@/shared/components/layout/AppLayout';
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute';
import { LoginForm } from '@/features/auth/components/LoginForm';
import { DashboardPage } from '@/features/dashboard/components/DashboardPage';
import { SubmissionList } from '@/features/submissions/components/SubmissionList';
import { UploadForm } from '@/features/submissions/components/UploadForm';
import { ReviewDetail } from '@/features/reviews/components/ReviewDetail';
import { CandidateList } from '@/features/candidates/components/CandidateList';
import { CandidateForm } from '@/features/candidates/components/CandidateForm';
import { ComparisonPage } from '@/features/comparison/components/ComparisonPage';
import { ScoringConfigEditor } from '@/features/scoring-config/components/ScoringConfigEditor';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginForm />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="submissions" element={<SubmissionList />} />
        <Route path="submissions/upload" element={<UploadForm />} />
        <Route path="submissions/:submissionId/review" element={<ReviewDetail />} />
        <Route path="candidates" element={<CandidateList />} />
        <Route path="candidates/new" element={<CandidateForm />} />
        <Route path="candidates/:candidateId/edit" element={<CandidateForm />} />
        <Route path="comparison" element={<ComparisonPage />} />
        <Route path="scoring-config" element={<ScoringConfigEditor />} />
      </Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
