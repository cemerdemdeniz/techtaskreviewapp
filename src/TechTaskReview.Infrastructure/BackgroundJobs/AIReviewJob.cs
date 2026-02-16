using System.Text.Json;
using Hangfire;
using Microsoft.Extensions.Logging;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Aggregates.Reviews;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Infrastructure.BackgroundJobs;

public class AIReviewJob
{
    private readonly ISubmissionRepository _submissions;
    private readonly ICodeReviewRepository _reviews;
    private readonly IScoringConfigRepository _scoringConfigs;
    private readonly IAIReviewService _aiReviewService;
    private readonly IAIProviderFactory _providerFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AIReviewJob> _logger;

    public AIReviewJob(
        ISubmissionRepository submissions, ICodeReviewRepository reviews,
        IScoringConfigRepository scoringConfigs, IAIReviewService aiReviewService,
        IAIProviderFactory providerFactory, IUnitOfWork unitOfWork, ILogger<AIReviewJob> logger)
    {
        _submissions = submissions; _reviews = reviews; _scoringConfigs = scoringConfigs;
        _aiReviewService = aiReviewService; _providerFactory = providerFactory;
        _unitOfWork = unitOfWork; _logger = logger;
    }

    [Queue("ai-review")]
    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 60, 300, 900, 1800, 3600 })]
    public async Task ExecuteAsync(Guid submissionId, string staticAnalysisJson, CancellationToken ct)
    {
        var submission = await _submissions.GetByIdAsync(submissionId, ct)
            ?? throw new InvalidOperationException($"Submission {submissionId} not found.");

        var provider = _providerFactory.GetProvider();
        var role = submission.CandidateId != Guid.Empty ? CandidateRole.Frontend : CandidateRole.Backend; // Simplified
        var config = await _scoringConfigs.GetDefaultForRoleAsync(role, ct)
            ?? throw new InvalidOperationException($"No default scoring config for role {role}");

        var review = CodeReview.Create(submission.Id, provider.Name, "default-model", config.Id);
        _reviews.Add(review);
        await _unitOfWork.SaveChangesAsync(ct);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await _aiReviewService.ReviewSubmissionAsync(
                submission, submission.Files.ToList(), staticAnalysisJson, role, ct);

            // Map category names to ScoreCategory enum and create CategoryScore entities
            foreach (var cat in result.Categories)
            {
                if (TryParseCategory(cat.Name, out var category))
                {
                    var score = CategoryScore.Create(
                        review.Id, category, cat.Score,
                        cat.Justification, cat.CriticalIssues,
                        cat.RefactorSuggestions, cat.SeniorImprovementPlan,
                        cat.Confidence);
                    review.AddCategoryScore(score);
                }
            }

            var weightedTotal = config.ComputeWeightedScore(review.CategoryScores);
            var summary = string.Join("\n\n", result.ChunkSummaries.Take(5));

            sw.Stop();
            review.Complete(weightedTotal, summary, JsonSerializer.Serialize(result),
                sw.Elapsed, result.ChunksProcessed, result.ChunksFailed);

            submission.MarkScoring();
            submission.MarkCompleted();
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "AI review failed for submission {Id}", submissionId);
            review.Fail(ex.Message, sw.Elapsed);
            submission.MarkFailed($"AI review failed: {ex.Message}");
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static bool TryParseCategory(string name, out ScoreCategory category)
    {
        var normalized = name.Replace("_", "").Replace(" ", "");
        return Enum.TryParse(normalized, ignoreCase: true, out category);
    }
}
