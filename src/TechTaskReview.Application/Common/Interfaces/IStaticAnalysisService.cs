namespace TechTaskReview.Application.Common.Interfaces;

public record StaticAnalysisResult(List<AnalyzerOutput> Outputs);
public record AnalyzerOutput(string AnalyzerName, int TotalIssues, int Errors, int Warnings, string RawOutput, List<AnalysisIssue> Issues)
{
    public bool IsFailed { get; init; }
    public string? FailureReason { get; init; }
    public static AnalyzerOutput Failed(string analyzerName, string reason) =>
        new(analyzerName, 0, 0, 0, string.Empty, []) { IsFailed = true, FailureReason = reason };
}
public record AnalysisIssue(string FilePath, int Line, string Severity, string RuleId, string Message);

public interface IStaticAnalysisService
{
    Task<StaticAnalysisResult> AnalyzeAsync(string extractedPath, TechTaskReview.Domain.Aggregates.Submissions.ProjectType projectType, CancellationToken ct);
}
