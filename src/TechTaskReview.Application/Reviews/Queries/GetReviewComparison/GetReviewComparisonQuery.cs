using MediatR;

namespace TechTaskReview.Application.Reviews.Queries.GetReviewComparison;

public record GetReviewComparisonQuery(Guid CandidateAId, Guid CandidateBId) : IRequest<ComparisonResultDto?>;

public record ComparisonResultDto(
    ComparisonCandidateDto CandidateA,
    ComparisonCandidateDto CandidateB,
    bool VersionMatch,
    string? VersionWarning,
    List<CategoryComparisonDto> Categories);

public record ComparisonCandidateDto(Guid Id, string Name, decimal TotalScore, string ReviewVersion);
public record CategoryComparisonDto(string Category, string Label, decimal? ScoreA, decimal? ScoreB, decimal Difference, string? Winner);
