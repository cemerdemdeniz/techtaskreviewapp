export interface SubmissionListItem {
  id: string;
  candidateFullName: string;
  candidateRole: string;
  status: SubmissionStatus;
  source: string;
  overallScore: number | null;
  createdAt: string;
  completedAt: string | null;
}

export type SubmissionStatus =
  | 'Uploaded'
  | 'Extracting'
  | 'Scanning'
  | 'Analyzing'
  | 'Reviewing'
  | 'Scoring'
  | 'Completed'
  | 'Failed'
  | 'PartiallyCompleted';

export interface SubmissionDetail extends SubmissionListItem {
  repositoryUrl: string | null;
  projectType: string;
  fileCount: number;
  totalSizeBytes: number;
  files: SubmissionFileItem[];
  errorMessage: string | null;
}

export interface SubmissionFileItem {
  id: string;
  relativePath: string;
  language: string;
  sizeBytes: number;
}

export interface CreateSubmissionRequest {
  candidateId: string;
  file: File;
}

export interface CreateGitSubmissionRequest {
  candidateId: string;
  repositoryUrl: string;
  branch?: string;
}
