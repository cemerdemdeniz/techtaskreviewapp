export interface ReviewDetail {
  id: string;
  submissionId: string;
  candidateFullName: string;
  candidateRole: string;
  status: string;
  overallScore: number;
  confidence: number;
  version: string;
  processingTimeSeconds: number;
  categoryScores: CategoryScoreItem[];
  createdAt: string;
  completedAt: string | null;
}

export interface CategoryScoreItem {
  category: string;
  categoryDisplayName: string;
  score: number;
  confidence: number;
  explanation: string;
  suggestions: string[];
}

export interface ReviewComparison {
  candidates: ReviewComparisonCandidate[];
  categories: string[];
}

export interface ReviewComparisonCandidate {
  candidateId: string;
  fullName: string;
  role: string;
  overallScore: number;
  scores: Record<string, number>;
}
