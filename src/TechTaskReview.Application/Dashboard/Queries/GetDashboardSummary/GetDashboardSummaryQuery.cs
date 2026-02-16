using MediatR;

namespace TechTaskReview.Application.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public record DashboardSummaryDto(
    int TotalCandidates,
    int TotalSubmissions,
    int CompletedReviews,
    int PendingReviews,
    int FailedReviews,
    decimal AverageScore,
    Dictionary<string, int> ScoreDistribution,
    List<RecentSubmissionDto> RecentSubmissions);

public record RecentSubmissionDto(
    Guid Id,
    string CandidateName,
    string Status,
    decimal? Score,
    DateTime CreatedAt);
