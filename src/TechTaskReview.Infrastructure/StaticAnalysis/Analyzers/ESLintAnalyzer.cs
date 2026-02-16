using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.StaticAnalysis.Analyzers;

public class ESLintAnalyzer : IStaticAnalyzer
{
    private readonly ILogger<ESLintAnalyzer> _logger;
    public string Name => "ESLint";

    public ESLintAnalyzer(ILogger<ESLintAnalyzer> logger) => _logger = logger;

    public bool CanAnalyze(ProjectType pt)
        => pt.PrimaryLanguage is "TypeScript" or "JavaScript";

    public async Task<AnalyzerOutput> RunAsync(string path, CancellationToken ct)
    {
        _logger.LogInformation("Running ESLint on {Path}", path);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"run --rm --read-only --network none --memory 512m -v {path}:/code:ro node:20-alpine npx --yes eslint /code --format json --no-eslintrc --ext .js,.jsx,.ts,.tsx 2>/dev/null || true",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        try
        {
            var eslintResults = JsonSerializer.Deserialize<JsonElement[]>(stdout);
            var errors = eslintResults?.Sum(r => r.GetProperty("errorCount").GetInt32()) ?? 0;
            var warnings = eslintResults?.Sum(r => r.GetProperty("warningCount").GetInt32()) ?? 0;

            return new AnalyzerOutput(Name, errors + warnings, errors, warnings, stdout, []);
        }
        catch
        {
            return new AnalyzerOutput(Name, 0, 0, 0, stdout, []);
        }
    }
}
