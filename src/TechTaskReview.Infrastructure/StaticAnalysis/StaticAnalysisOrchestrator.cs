using Microsoft.Extensions.Logging;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.StaticAnalysis;

public class StaticAnalysisOrchestrator : IStaticAnalysisService
{
    private readonly IEnumerable<IStaticAnalyzer> _analyzers;
    private readonly ILogger<StaticAnalysisOrchestrator> _logger;

    public StaticAnalysisOrchestrator(IEnumerable<IStaticAnalyzer> analyzers, ILogger<StaticAnalysisOrchestrator> logger)
    {
        _analyzers = analyzers;
        _logger = logger;
    }

    public async Task<StaticAnalysisResult> AnalyzeAsync(string extractedPath, ProjectType projectType, CancellationToken ct)
    {
        var applicable = _analyzers.Where(a => a.CanAnalyze(projectType)).ToList();
        _logger.LogInformation("Running {Count} analyzers for {Language}/{Framework}",
            applicable.Count, projectType.PrimaryLanguage, projectType.Framework);

        var results = new List<AnalyzerOutput>();

        foreach (var analyzer in applicable)
        {
            try
            {
                var output = await analyzer.RunAsync(extractedPath, ct);
                results.Add(output);
                _logger.LogInformation("Analyzer {Name}: {Errors} errors, {Warnings} warnings",
                    analyzer.Name, output.Errors, output.Warnings);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Analyzer {Name} failed", analyzer.Name);
                results.Add(AnalyzerOutput.Failed(analyzer.Name, ex.Message));
            }
        }

        return new StaticAnalysisResult(results);
    }
}
