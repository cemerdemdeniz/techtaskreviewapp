using MediatR;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Application.Reviews.Queries.GetReview;

public class GetReviewQueryHandler : IRequestHandler<GetReviewQuery, ReviewDetailDto?>
{
    private readonly ICodeReviewRepository _reviews;
    private readonly ISubmissionRepository _submissions;
    private readonly ICandidateRepository _candidates;
    private readonly IScoringConfigRepository _scoringConfigs;

    public GetReviewQueryHandler(
        ICodeReviewRepository reviews,
        ISubmissionRepository submissions,
        ICandidateRepository candidates,
        IScoringConfigRepository scoringConfigs)
    {
        _reviews = reviews;
        _submissions = submissions;
        _candidates = candidates;
        _scoringConfigs = scoringConfigs;
    }

    public async Task<ReviewDetailDto?> Handle(GetReviewQuery request, CancellationToken ct)
    {
        var review = await _reviews.GetByIdAsync(request.ReviewId, ct);
        if (review is null) return null;

        var submission = await _submissions.GetByIdAsync(review.SubmissionId, ct);
        var candidate = submission is not null ? await _candidates.GetByIdAsync(submission.CandidateId, ct) : null;
        var config = await _scoringConfigs.GetByIdAsync(review.ScoringConfigId, ct);

        var categoryDtos = review.CategoryScores.Select(cs =>
        {
            var weight = config?.Weights.FirstOrDefault(w => w.Category == cs.Category);
            return new CategoryScoreDto(
                cs.Category.ToString(),
                FormatCategoryName(cs.Category.ToString()),
                cs.Score,
                weight?.Weight ?? 0,
                cs.Confidence,
                cs.Justification,
                cs.CriticalIssues,
                cs.RefactorSuggestions,
                cs.SeniorLevelImprovementPlan);
        }).ToList();

        return new ReviewDetailDto(
            review.Id,
            review.SubmissionId,
            submission?.CandidateId ?? Guid.Empty,
            candidate?.FullName ?? "Unknown",
            review.Version.ToString(),
            review.AIProviderName,
            review.AIModelName,
            review.Status.ToString(),
            review.WeightedTotalScore,
            review.Summary,
            categoryDtos,
            review.StaticAnalysisResultsJson,
            review.ChunksProcessed,
            review.ChunksFailed,
            review.ProcessingDuration.ToString(),
            review.CreatedAt);
    }

    private static string FormatCategoryName(string category)
    {
        return string.Concat(category.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }
}
