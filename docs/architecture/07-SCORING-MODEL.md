# 07 — Scoring Model Design

## Mathematical Model

### Weighted Score Computation

Given:
- `N` scored categories for a submission
- `s_i` = raw score for category `i` (0.0 – 10.0)
- `w_i` = weight for category `i` (0.0 – 1.0)
- `c_i` = AI confidence for category `i` (0.0 – 1.0)

**Weighted Total Score**:

```
                 Σ (s_i × w_i × c_i)
  S_total = ────────────────────────────
                   Σ (w_i × c_i)
```

This is a confidence-weighted average. Categories where the AI is uncertain (low confidence) have proportionally less influence on the final score.

**Why confidence-weighted?** When reviewing chunked code, the AI may encounter a category (e.g., "Accessibility") in only one chunk and have low confidence. Without confidence weighting, a single uncertain data point could distort the score the same as a well-evidenced one.

### Normalization

Scores are already on a 0-10 scale, so no min-max normalization is needed. However, we apply:

1. **Floor clamping**: If any category with a `MinimumPassingScore` fails, the total score is capped at 5.0 regardless of the weighted average. This prevents a candidate who writes beautiful code but has zero security from scoring 8/10.

2. **Confidence penalty**: If average confidence across categories is below 0.5, the total score receives a 10% penalty (multiplied by 0.9). This discourages trusting results where the AI was guessing.

```csharp
public class ScoreComputer
{
    public ScoringResult Compute(
        IReadOnlyList<CategoryScore> scores,
        ScoringConfig config)
    {
        decimal weightedSum = 0;
        decimal totalWeight = 0;
        var failingCategories = new List<(ScoreCategory, decimal, decimal)>();

        foreach (var weight in config.Weights)
        {
            var score = scores.FirstOrDefault(s => s.Category == weight.Category);
            if (score is null) continue;

            var effectiveWeight = weight.Weight * score.Confidence;
            weightedSum += score.Score * effectiveWeight;
            totalWeight += effectiveWeight;

            // Check minimum passing score
            if (weight.MinimumPassingScore.HasValue && score.Score < weight.MinimumPassingScore.Value)
            {
                failingCategories.Add((weight.Category, score.Score, weight.MinimumPassingScore.Value));
            }
        }

        var rawTotal = totalWeight > 0 ? weightedSum / totalWeight : 0m;

        // Apply floor cap if critical categories fail
        if (failingCategories.Count > 0)
        {
            rawTotal = Math.Min(rawTotal, 5.0m);
        }

        // Apply confidence penalty
        var avgConfidence = scores.Average(s => s.Confidence);
        if (avgConfidence < 0.5m)
        {
            rawTotal *= 0.9m;
        }

        return new ScoringResult(
            WeightedTotal: Math.Round(rawTotal, 2),
            CategoryResults: scores.Select(s => new CategoryResult(
                s.Category, s.Score,
                config.Weights.FirstOrDefault(w => w.Category == s.Category)?.Weight ?? 0,
                s.Confidence
            )).ToList(),
            FailingCategories: failingCategories,
            AverageConfidence: Math.Round(avgConfidence, 2),
            ConfidencePenaltyApplied: avgConfidence < 0.5m,
            FloorCapApplied: failingCategories.Count > 0
        );
    }
}
```

## Default Weight Configurations

### Frontend Engineer Weights

| Category | Weight | Min Passing | Rationale |
|----------|--------|-------------|-----------|
| Code Quality | 0.12 | — | Universal baseline |
| Architecture | 0.10 | — | Overall structure |
| Maintainability | 0.08 | — | Long-term cost |
| Naming | 0.04 | — | Readability signal |
| Error Handling | 0.06 | — | Robustness |
| Security | 0.06 | 3.0 | Non-negotiable floor |
| Performance | 0.06 | — | User experience |
| Testability | 0.05 | — | Engineering maturity |
| **Component Design** | **0.12** | — | Core frontend skill |
| **State Management** | **0.10** | — | Core frontend skill |
| **Hooks Usage** | **0.08** | — | React proficiency |
| **Re-render Optimization** | **0.05** | — | Advanced signal |
| **Accessibility** | **0.04** | 2.0 | Legal/ethical floor |
| **Responsiveness** | **0.04** | — | Expected baseline |
| **Total** | **1.00** | | |

### Backend Engineer Weights

| Category | Weight | Min Passing | Rationale |
|----------|--------|-------------|-----------|
| Code Quality | 0.10 | — | Universal baseline |
| Architecture | 0.10 | — | System design signal |
| Maintainability | 0.08 | — | Long-term cost |
| Naming | 0.04 | — | Readability |
| Error Handling | 0.08 | — | Reliability |
| Security | 0.08 | 4.0 | Critical for backend |
| Performance | 0.06 | — | Scalability signal |
| Testability | 0.06 | — | Engineering maturity |
| **API Design** | **0.10** | — | Core backend skill |
| **Layered Architecture** | **0.08** | — | Structure discipline |
| **Dependency Injection** | **0.06** | — | .NET proficiency |
| **Async Correctness** | **0.06** | 3.0 | Critical for .NET |
| **Security Practices** | **0.06** | 4.0 | Non-negotiable |
| **DB Interaction Patterns** | **0.04** | — | Data layer skill |
| **Total** | **1.00** | | |

## Bias Mitigation Strategy

### Problem

AI scoring can exhibit biases:
1. **Verbosity bias**: Longer code files may get higher scores simply because there's "more to praise"
2. **Style bias**: AI may prefer certain coding styles (e.g., functional vs OOP)
3. **Framework bias**: AI may score familiar frameworks higher
4. **Positional bias**: First/last chunks may get different treatment
5. **Confidence inflation**: AI tends to report higher confidence than warranted

### Mitigations

| Bias | Mitigation | Implementation |
|------|-----------|----------------|
| Verbosity | Normalize by file count — don't reward volume | Score per-category, not per-file |
| Style | Prompt explicitly states "do not penalize valid style choices" | Prompt engineering |
| Framework | Categories are framework-agnostic (e.g., "State Management" not "Redux usage") | Category design |
| Positional | Shuffle chunk order before dispatch | Randomize in `AIReviewService` |
| Confidence inflation | Apply confidence penalty below 0.5; track distribution over time | Score computation + monitoring |
| Score inflation | Prompt calibration: "Most average code should score 5-6" | Prompt engineering |

### Calibration Process

1. **Baseline corpus**: Maintain 20 reference submissions (10 frontend, 10 backend) with human-assigned scores
2. **Drift detection**: Monthly batch re-review of baseline corpus; if AI scores drift >1.0 from human scores on average, trigger prompt recalibration
3. **A/B testing**: When changing prompts or models, run both old and new on baseline corpus to measure impact

```csharp
// Calibration check (run as scheduled Hangfire job monthly)
public class CalibrationCheckJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var baselineSubmissions = await _repository.GetBaselineSubmissionsAsync(ct);

        foreach (var baseline in baselineSubmissions)
        {
            var currentReview = await _aiReviewService.ReviewSubmissionAsync(
                baseline.Submission, baseline.Files, null, baseline.Role, ct);

            var drift = CalculateDrift(baseline.HumanScores, currentReview.Categories);

            if (drift.AverageDrift > 1.0m)
            {
                await _alertService.SendCalibrationDriftAlert(drift, ct);
            }

            await _metricsRepository.StoreCalibrationResult(baseline.Id, drift, ct);
        }
    }
}
```

## Score Versioning

Every `CodeReview` records:
- `ReviewVersion` (major.minor + scoring model version)
- AI provider and model name
- Scoring config ID (which weights were used)

When weights or prompts change:
- **Minor version bump** (e.g., 1.0 → 1.1): Prompt wording changes, weight adjustments <10%
- **Major version bump** (e.g., 1.x → 2.0): New categories added, model change, fundamental prompt restructure

Historical scores are never retroactively modified. Recruiter dashboard shows version alongside score, and comparison views only compare candidates scored with the same version.

## Score Interpretation Guide (for recruiters)

| Score Range | Label | Recommendation |
|-------------|-------|---------------|
| 8.5 – 10.0 | Exceptional | Strong hire signal. Proceed to final interview. |
| 7.0 – 8.4 | Strong | Good hire signal. Minor gaps addressable on the job. |
| 5.5 – 6.9 | Adequate | Average. Dig deeper in interview on weak categories. |
| 4.0 – 5.4 | Below Average | Concerns. Review specific category failures. |
| 0.0 – 3.9 | Insufficient | Significant gaps. Likely reject unless extenuating context. |

These thresholds are configurable per `ScoringConfig`.

## Comparison Algorithm

When comparing two candidates:

```csharp
public class CandidateComparisonService
{
    public ComparisonResult Compare(CodeReview reviewA, CodeReview reviewB)
    {
        var categoryComparisons = new List<CategoryComparison>();

        var allCategories = reviewA.CategoryScores
            .Select(s => s.Category)
            .Union(reviewB.CategoryScores.Select(s => s.Category))
            .Distinct();

        foreach (var category in allCategories)
        {
            var scoreA = reviewA.CategoryScores.FirstOrDefault(s => s.Category == category);
            var scoreB = reviewB.CategoryScores.FirstOrDefault(s => s.Category == category);

            categoryComparisons.Add(new CategoryComparison(
                Category: category,
                CandidateAScore: scoreA?.Score,
                CandidateBScore: scoreB?.Score,
                Difference: (scoreA?.Score ?? 0) - (scoreB?.Score ?? 0),
                Winner: DetermineWinner(scoreA?.Score, scoreB?.Score)
            ));
        }

        return new ComparisonResult(
            CandidateATotal: reviewA.WeightedTotalScore,
            CandidateBTotal: reviewB.WeightedTotalScore,
            Categories: categoryComparisons,
            VersionMatch: reviewA.Version.Equals(reviewB.Version),
            VersionWarning: !reviewA.Version.Equals(reviewB.Version)
                ? "Candidates were scored with different review versions. Comparison may not be fully reliable."
                : null
        );
    }
}
```
