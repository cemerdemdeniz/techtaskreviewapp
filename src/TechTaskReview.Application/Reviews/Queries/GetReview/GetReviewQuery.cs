using MediatR;

namespace TechTaskReview.Application.Reviews.Queries.GetReview;

public record GetReviewQuery(Guid ReviewId) : IRequest<ReviewDetailDto?>;

public record ReviewDetailDto(
    Guid Id,
    Guid SubmissionId,
    Guid CandidateId,
    string CandidateName,
    string Version,
    string AIProvider,
    string AIModel,
    string Status,
    decimal WeightedTotalScore,
    string? Summary,
    List<CategoryScoreDto> CategoryScores,
    string? StaticAnalysisJson,
    int ChunksProcessed,
    int ChunksFailed,
    string ProcessingDuration,
    DateTime CreatedAt);

public record CategoryScoreDto(
    string Category,
    string CategoryLabel,
    decimal Score,
    decimal Weight,
    decimal Confidence,
    string Justification,
    string[] CriticalIssues,
    string[] RefactorSuggestions,
    string SeniorImprovementPlan);
