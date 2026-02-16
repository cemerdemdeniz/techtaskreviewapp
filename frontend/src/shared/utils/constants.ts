export const APP_NAME = 'TechTask Review';

export const SUBMISSION_STATUSES = {
  PENDING: 'pending',
  PROCESSING: 'processing',
  COMPLETED: 'completed',
  FAILED: 'failed',
} as const;

export type SubmissionStatusValue =
  (typeof SUBMISSION_STATUSES)[keyof typeof SUBMISSION_STATUSES];

export const SCORE_CATEGORIES = [
  'code_quality',
  'architecture',
  'testing',
  'documentation',
  'performance',
  'security',
  'best_practices',
] as const;

export type ScoreCategory = (typeof SCORE_CATEGORIES)[number];

export const SCORE_CATEGORY_LABELS: Record<ScoreCategory, string> = {
  code_quality: 'Code Quality',
  architecture: 'Architecture',
  testing: 'Testing',
  documentation: 'Documentation',
  performance: 'Performance',
  security: 'Security',
  best_practices: 'Best Practices',
};

export const POLLING_INTERVAL = 3000;

export const PAGE_SIZES = [10, 25, 50] as const;

export const MAX_FILE_SIZE = 50 * 1024 * 1024; // 50MB
