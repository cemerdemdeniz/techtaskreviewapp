using TechTaskReview.Domain.Common;
using TechTaskReview.Domain.Exceptions;

namespace TechTaskReview.Domain.Aggregates.Reviews;

public class CategoryScore : Entity
{
    public Guid CodeReviewId { get; private set; }
    public ScoreCategory Category { get; private set; }
    public decimal Score { get; private set; }              // 0.0 - 10.0
    public string Justification { get; private set; } = null!;
    public string[] CriticalIssues { get; private set; } = [];
    public string[] RefactorSuggestions { get; private set; } = [];
    public string SeniorLevelImprovementPlan { get; private set; } = null!;
    public decimal Confidence { get; private set; }         // 0.0 - 1.0 (AI self-reported)

    private CategoryScore() { }

    public static CategoryScore Create(
        Guid codeReviewId,
        ScoreCategory category,
        decimal score,
        string justification,
        string[] criticalIssues,
        string[] refactorSuggestions,
        string seniorPlan,
        decimal confidence)
    {
        if (score < 0 || score > 10)
            throw new DomainException($"Score must be between 0 and 10. Got: {score}");
        if (confidence < 0 || confidence > 1)
            throw new DomainException($"Confidence must be between 0 and 1. Got: {confidence}");

        return new CategoryScore
        {
            CodeReviewId = codeReviewId,
            Category = category,
            Score = Math.Round(score, 1),
            Justification = justification,
            CriticalIssues = criticalIssues,
            RefactorSuggestions = refactorSuggestions,
            SeniorLevelImprovementPlan = seniorPlan,
            Confidence = Math.Round(confidence, 2)
        };
    }
}
