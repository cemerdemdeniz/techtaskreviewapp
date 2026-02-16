using MediatR;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Application.Reviews.Queries.GetReviewComparison;

public class GetReviewComparisonQueryHandler : IRequestHandler<GetReviewComparisonQuery, ComparisonResultDto?>
{
    private readonly ICandidateRepository _candidates;
    private readonly ISubmissionRepository _submissions;
    private readonly ICodeReviewRepository _reviews;

    public GetReviewComparisonQueryHandler(
        ICandidateRepository candidates,
        ISubmissionRepository submissions,
        ICodeReviewRepository reviews)
    {
        _candidates = candidates;
        _submissions = submissions;
        _reviews = reviews;
    }

    public async Task<ComparisonResultDto?> Handle(GetReviewComparisonQuery request, CancellationToken ct)
    {
        var candidateA = await _candidates.GetByIdAsync(request.CandidateAId, ct);
        var candidateB = await _candidates.GetByIdAsync(request.CandidateBId, ct);
        if (candidateA is null || candidateB is null) return null;

        var subsA = await _submissions.GetByCandidateIdAsync(request.CandidateAId, ct);
        var subsB = await _submissions.GetByCandidateIdAsync(request.CandidateBId, ct);

        var latestA = subsA.Where(s => s.Status == SubmissionStatus.Completed && s.Review is not null)
            .OrderByDescending(s => s.CreatedAt).FirstOrDefault();
        var latestB = subsB.Where(s => s.Status == SubmissionStatus.Completed && s.Review is not null)
            .OrderByDescending(s => s.CreatedAt).FirstOrDefault();

        if (latestA?.Review is null || latestB?.Review is null) return null;

        var reviewA = latestA.Review;
        var reviewB = latestB.Review;

        var allCategories = reviewA.CategoryScores.Select(s => s.Category)
            .Union(reviewB.CategoryScores.Select(s => s.Category))
            .Distinct();

        var comparisons = allCategories.Select(cat =>
        {
            var scoreA = reviewA.CategoryScores.FirstOrDefault(s => s.Category == cat)?.Score;
            var scoreB = reviewB.CategoryScores.FirstOrDefault(s => s.Category == cat)?.Score;
            var diff = (scoreA ?? 0) - (scoreB ?? 0);

            return new CategoryComparisonDto(
                cat.ToString(),
                string.Concat(cat.ToString().Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString())),
                scoreA, scoreB, diff,
                diff > 0.5m ? "A" : diff < -0.5m ? "B" : null);
        }).ToList();

        var versionMatch = reviewA.Version.Equals(reviewB.Version);

        return new ComparisonResultDto(
            new ComparisonCandidateDto(candidateA.Id, candidateA.FullName, reviewA.WeightedTotalScore, reviewA.Version.ToString()),
            new ComparisonCandidateDto(candidateB.Id, candidateB.FullName, reviewB.WeightedTotalScore, reviewB.Version.ToString()),
            versionMatch,
            versionMatch ? null : "Candidates were scored with different review versions. Comparison may not be fully reliable.",
            comparisons);
    }
}
