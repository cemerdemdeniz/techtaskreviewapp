export interface DashboardSummary {
  totalSubmissions: number;
  pendingReviews: number;
  completedReviews: number;
  failedReviews: number;
  averageScore: number;
  averageProcessingTimeMinutes: number;
  submissionsByDay: DailySubmissionCount[];
  scoreDistribution: ScoreDistributionItem[];
  topCandidates: TopCandidate[];
}

export interface DailySubmissionCount {
  date: string;
  count: number;
}

export interface ScoreDistributionItem {
  range: string;
  count: number;
}

export interface TopCandidate {
  candidateId: string;
  fullName: string;
  role: string;
  score: number;
  submittedAt: string;
}
