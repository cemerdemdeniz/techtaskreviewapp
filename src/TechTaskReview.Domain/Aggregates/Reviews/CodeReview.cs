using TechTaskReview.Domain.Common;
using TechTaskReview.Domain.Exceptions;

namespace TechTaskReview.Domain.Aggregates.Reviews;

public class CodeReview : AggregateRoot
{
    public Guid SubmissionId { get; private set; }
    public ReviewVersion Version { get; private set; } = null!;
    public string AIProviderName { get; private set; } = null!;
    public string AIModelName { get; private set; } = null!;
    public ReviewStatus Status { get; private set; }
    public decimal WeightedTotalScore { get; private set; }
    public string? Summary { get; private set; }
    public string? RawResponseJson { get; private set; }
    public string? StaticAnalysisResultsJson { get; private set; }
    public int ChunksProcessed { get; private set; }
    public int ChunksFailed { get; private set; }
    public TimeSpan ProcessingDuration { get; private set; }
    public Guid ScoringConfigId { get; private set; }

    private readonly List<CategoryScore> _categoryScores = [];
    public IReadOnlyList<CategoryScore> CategoryScores => _categoryScores.AsReadOnly();

    private CodeReview() { }

    public static CodeReview Create(
        Guid submissionId, string providerName, string modelName, Guid scoringConfigId)
    {
        return new CodeReview
        {
            SubmissionId = submissionId,
            Version = ReviewVersion.Current,
            AIProviderName = providerName,
            AIModelName = modelName,
            Status = ReviewStatus.InProgress,
            ScoringConfigId = scoringConfigId
        };
    }

    public void AddCategoryScore(CategoryScore score)
    {
        if (_categoryScores.Any(s => s.Category == score.Category))
            throw new DomainException($"Score for category {score.Category} already exists.");
        _categoryScores.Add(score);
    }

    public void Complete(decimal weightedTotal, string summary, string rawJson, TimeSpan duration,
        int chunksProcessed, int chunksFailed)
    {
        WeightedTotalScore = weightedTotal;
        Summary = summary;
        RawResponseJson = rawJson;
        ProcessingDuration = duration;
        ChunksProcessed = chunksProcessed;
        ChunksFailed = chunksFailed;
        Status = chunksFailed > 0 ? ReviewStatus.Partial : ReviewStatus.Completed;
    }

    public void Fail(string rawJson, TimeSpan duration)
    {
        RawResponseJson = rawJson;
        ProcessingDuration = duration;
        Status = ReviewStatus.Failed;
    }

    public void SetStaticAnalysisResults(string json)
        => StaticAnalysisResultsJson = json;
}
