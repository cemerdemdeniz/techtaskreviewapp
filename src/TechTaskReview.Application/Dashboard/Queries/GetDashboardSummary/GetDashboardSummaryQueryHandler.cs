using MediatR;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Application.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly ISubmissionRepository _submissions;
    private readonly ICandidateRepository _candidates;
    private readonly ICacheService _cache;

    public GetDashboardSummaryQueryHandler(
        ISubmissionRepository submissions,
        ICandidateRepository candidates,
        ICacheService cache)
    {
        _submissions = submissions;
        _candidates = candidates;
        _cache = cache;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        return await _cache.GetOrSetAsync("dashboard:summary", async () =>
        {
            var allCandidates = await _candidates.GetPaginatedAsync(1, 1, null, null, ct);
            var allSubmissions = await _submissions.GetPaginatedAsync(1, 1, null, ct);
            var completed = await _submissions.GetPaginatedAsync(1, 1, SubmissionStatus.Completed, ct);
            var failed = await _submissions.GetPaginatedAsync(1, 1, SubmissionStatus.Failed, ct);

            var recentCompleted = await _submissions.GetPaginatedAsync(1, 10, SubmissionStatus.Completed, ct);

            var scores = recentCompleted.Items
                .Where(s => s.Review is not null)
                .Select(s => s.Review!.WeightedTotalScore)
                .ToList();

            var avgScore = scores.Count > 0 ? scores.Average() : 0m;

            var distribution = new Dictionary<string, int>
            {
                ["0-2"] = scores.Count(s => s < 2),
                ["2-4"] = scores.Count(s => s >= 2 && s < 4),
                ["4-6"] = scores.Count(s => s >= 4 && s < 6),
                ["6-8"] = scores.Count(s => s >= 6 && s < 8),
                ["8-10"] = scores.Count(s => s >= 8)
            };

            var pendingCount = allSubmissions.TotalCount - completed.TotalCount - failed.TotalCount;

            var recentDtos = recentCompleted.Items.Select(s => new RecentSubmissionDto(
                s.Id, string.Empty, s.Status.ToString(),
                s.Review?.WeightedTotalScore, s.CreatedAt
            )).ToList();

            return new DashboardSummaryDto(
                allCandidates.TotalCount,
                allSubmissions.TotalCount,
                completed.TotalCount,
                pendingCount,
                failed.TotalCount,
                Math.Round(avgScore, 2),
                distribution,
                recentDtos);
        }, TimeSpan.FromSeconds(60), ct) ?? new DashboardSummaryDto(0, 0, 0, 0, 0, 0, new(), []);
    }
}
