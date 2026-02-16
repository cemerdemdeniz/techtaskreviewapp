using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.StaticAnalysis.Analyzers;

public class DotNetAnalyzer : IStaticAnalyzer
{
    private readonly ILogger<DotNetAnalyzer> _logger;
    public string Name => "dotnet-analyzers";

    public DotNetAnalyzer(ILogger<DotNetAnalyzer> logger) => _logger = logger;

    public bool CanAnalyze(ProjectType pt) => pt.PrimaryLanguage == "C#";

    public async Task<AnalyzerOutput> RunAsync(string path, CancellationToken ct)
    {
        _logger.LogInformation("Running dotnet build analyzers on {Path}", path);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"run --rm --read-only --network none --memory 1g -v {path}:/code:ro mcr.microsoft.com/dotnet/sdk:8.0 dotnet build /code --no-restore -v q 2>&1 || true",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var output = stdout + stderr;
        var errorLines = output.Split('\n').Count(l => l.Contains(": error ", StringComparison.OrdinalIgnoreCase));
        var warningLines = output.Split('\n').Count(l => l.Contains(": warning ", StringComparison.OrdinalIgnoreCase));

        return new AnalyzerOutput(Name, errorLines + warningLines, errorLines, warningLines, output, []);
    }
}
