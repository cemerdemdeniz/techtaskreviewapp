export interface CandidateListItem {
  id: string;
  fullName: string;
  email: string;
  role: string;
  submissionCount: number;
  latestScore: number | null;
  createdAt: string;
}

export interface CandidateDetail extends CandidateListItem {
  submissions: CandidateSubmission[];
}

export interface CandidateSubmission {
  id: string;
  status: string;
  overallScore: number | null;
  createdAt: string;
}

export interface CreateCandidateRequest {
  firstName: string;
  lastName: string;
  email: string;
  role: 'Frontend' | 'Backend';
}
