using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Application.Common.Interfaces;

public record AggregatedReviewResult(
    List<AggregatedCategoryScore> Categories,
    List<string> ChunkSummaries,
    List<string> CriticalFindings,
    int ChunksProcessed,
    int ChunksFailed);

public record AggregatedCategoryScore(
    string Name,
    decimal Score,
    string Justification,
    string[] CriticalIssues,
    string[] RefactorSuggestions,
    string SeniorImprovementPlan,
    decimal Confidence,
    int ChunksCovered);

public interface IAIReviewService
{
    Task<AggregatedReviewResult> ReviewSubmissionAsync(
        Submission submission,
        List<SubmissionFile> files,
        string? staticAnalysisJson,
        CandidateRole role,
        CancellationToken ct);
}
