using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Aggregates.Submissions;
using TechTaskReview.Infrastructure.AI.Chunking;
using TechTaskReview.Infrastructure.AI.Prompts;

namespace TechTaskReview.Infrastructure.AI;

public class AIReviewService : IAIReviewService
{
    private readonly IAIProviderFactory _providerFactory;
    private readonly IFileChunker _chunker;
    private readonly PromptTemplateService _promptService;
    private readonly AIProviderOptions _options;
    private readonly ILogger<AIReviewService> _logger;

    public AIReviewService(
        IAIProviderFactory providerFactory,
        IFileChunker chunker,
        PromptTemplateService promptService,
        IOptions<AIProviderOptions> options,
        ILogger<AIReviewService> logger)
    {
        _providerFactory = providerFactory;
        _chunker = chunker;
        _promptService = promptService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AggregatedReviewResult> ReviewSubmissionAsync(
        Submission submission, List<SubmissionFile> files, string? staticAnalysisJson,
        CandidateRole role, CancellationToken ct)
    {
        var provider = _providerFactory.GetProvider();
        var fileForReview = SelectFiles(files);
        var chunks = _chunker.CreateChunks(fileForReview, submission.ExtractedPath!);

        _logger.LogInformation("Reviewing submission {Id}: {ChunkCount} chunks via {Provider}",
            submission.Id, chunks.Count, provider.Name);

        var semaphore = new SemaphoreSlim(_options.MaxConcurrentRequests);
        var results = new ConcurrentBag<ChunkResult>();
        int failedChunks = 0;

        var tasks = chunks.Select(async chunk =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var prompt = _promptService.BuildChunkReviewPrompt(
                    chunk, role, submission.DetectedProjectType!, staticAnalysisJson);

                var response = await provider.ReviewCodeAsync(
                    new AIReviewRequest(prompt, _options.Temperature, _options.MaxResponseTokens), ct);

                var parsed = ParseChunkResponse(response.Content, chunk.ChunkIndex);
                results.Add(parsed);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failedChunks);
                _logger.LogWarning(ex, "Chunk {Index} failed", chunk.ChunkIndex);
                results.Add(new ChunkResult { ChunkIndex = chunk.ChunkIndex, IsFailed = true, FailureReason = ex.Message });
            }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);
        return AggregateResults(results.ToList(), failedChunks, chunks.Count);
    }

    private static List<FileForReview> SelectFiles(List<SubmissionFile> files)
    {
        var result = new List<FileForReview>();
        var reviewable = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "TypeScript", "JavaScript", "C#", "Python", "Java", "Go" };

        var core = files.Where(f => !f.IsTestFile && !f.IsConfigFile && reviewable.Contains(f.Language))
            .OrderByDescending(f => f.LineCount);
        var tests = files.Where(f => f.IsTestFile && reviewable.Contains(f.Language))
            .OrderByDescending(f => f.LineCount).Take(Math.Max(5, files.Count / 5));
        var configs = files.Where(f => f.IsConfigFile && IsImportantConfig(f.RelativePath));

        result.AddRange(core.Select(f => new FileForReview(f, FilePriority.Core)));
        result.AddRange(tests.Select(f => new FileForReview(f, FilePriority.Test)));
        result.AddRange(configs.Select(f => new FileForReview(f, FilePriority.Config)));
        return result;
    }

    private static bool IsImportantConfig(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        return name is "tsconfig.json" or "package.json" or ".eslintrc.json" or "dockerfile" or "docker-compose.yml"
            || name.EndsWith(".csproj");
    }

    private ChunkResult ParseChunkResponse(string content, int chunkIndex)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            var categories = json.GetProperty("categories").EnumerateArray().Select(c => new ChunkCategoryResult
            {
                Name = c.GetProperty("name").GetString() ?? "",
                Score = c.TryGetProperty("score", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetDecimal() : null,
                Justification = c.GetProperty("justification").GetString() ?? "",
                CriticalIssues = c.TryGetProperty("critical_issues", out var ci)
                    ? ci.EnumerateArray().Select(x => x.GetString() ?? "").ToArray() : [],
                RefactorSuggestions = c.TryGetProperty("refactor_suggestions", out var rs)
                    ? rs.EnumerateArray().Select(x => x.GetString() ?? "").ToArray() : [],
                SeniorImprovementPlan = c.TryGetProperty("senior_improvement_plan", out var sp) ? sp.GetString() ?? "" : "",
                Confidence = c.TryGetProperty("confidence", out var conf) && conf.ValueKind == JsonValueKind.Number ? conf.GetDecimal() : 0.5m,
            }).ToList();

            var summary = json.TryGetProperty("chunk_summary", out var cs) ? cs.GetString() ?? "" : "";
            var critical = json.TryGetProperty("critical_findings", out var cf)
                ? cf.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>();

            return new ChunkResult { ChunkIndex = chunkIndex, Categories = categories, ChunkSummary = summary, CriticalFindings = critical };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response for chunk {Index}", chunkIndex);
            return new ChunkResult { ChunkIndex = chunkIndex, IsFailed = true, FailureReason = $"Parse error: {ex.Message}" };
        }
    }

    private static AggregatedReviewResult AggregateResults(List<ChunkResult> results, int failedChunks, int totalChunks)
    {
        var successful = results.Where(r => !r.IsFailed).ToList();

        var categoryAggregates = successful
            .SelectMany(r => r.Categories)
            .Where(c => c.Score.HasValue)
            .GroupBy(c => c.Name)
            .Select(g =>
            {
                var scores = g.ToList();
                var weightedSum = scores.Sum(s => s.Score!.Value * s.Confidence);
                var totalWeight = scores.Sum(s => s.Confidence);
                var avgScore = totalWeight > 0 ? weightedSum / totalWeight : 0;

                return new AggregatedCategoryScore(
                    g.Key,
                    Math.Round(avgScore, 1),
                    scores.OrderByDescending(s => s.Justification.Length).First().Justification,
                    scores.SelectMany(s => s.CriticalIssues).Distinct().ToArray(),
                    scores.SelectMany(s => s.RefactorSuggestions).Distinct().ToArray(),
                    string.Join("\n\n", scores.Select(s => s.SeniorImprovementPlan).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct()),
                    Math.Round(scores.Average(s => s.Confidence), 2),
                    scores.Count);
            }).ToList();

        return new AggregatedReviewResult(
            categoryAggregates,
            successful.Select(r => r.ChunkSummary).Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
            successful.SelectMany(r => r.CriticalFindings).Distinct().ToList(),
            totalChunks - failedChunks,
            failedChunks);
    }

    private class ChunkResult
    {
        public int ChunkIndex { get; init; }
        public bool IsFailed { get; init; }
        public string? FailureReason { get; init; }
        public List<ChunkCategoryResult> Categories { get; init; } = [];
        public string ChunkSummary { get; init; } = "";
        public List<string> CriticalFindings { get; init; } = [];
    }

    private class ChunkCategoryResult
    {
        public string Name { get; init; } = "";
        public decimal? Score { get; init; }
        public string Justification { get; init; } = "";
        public string[] CriticalIssues { get; init; } = [];
        public string[] RefactorSuggestions { get; init; } = [];
        public string SeniorImprovementPlan { get; init; } = "";
        public decimal Confidence { get; init; } = 0.5m;
    }
}
