export interface ScoringConfig {
  id: string;
  role: string;
  weights: CategoryWeightItem[];
}

export interface CategoryWeightItem {
  category: string;
  categoryDisplayName: string;
  weight: number;
  minPassingScore: number;
}

export interface UpdateScoringConfigRequest {
  weights: { category: string; weight: number; minPassingScore: number }[];
}
