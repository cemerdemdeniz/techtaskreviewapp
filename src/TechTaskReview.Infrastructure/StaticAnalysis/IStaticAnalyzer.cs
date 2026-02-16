using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.StaticAnalysis;

public interface IStaticAnalyzer
{
    string Name { get; }
    bool CanAnalyze(ProjectType projectType);
    Task<AnalyzerOutput> RunAsync(string extractedPath, CancellationToken ct);
}
