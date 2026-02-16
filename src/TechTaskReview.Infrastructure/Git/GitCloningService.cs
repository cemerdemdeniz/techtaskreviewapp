using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Exceptions;

namespace TechTaskReview.Infrastructure.Git;

public class GitCloningService : IGitCloningService
{
    private readonly GitCloningOptions _options;
    private readonly ILogger<GitCloningService> _logger;

    public GitCloningService(IOptions<GitCloningOptions> options, ILogger<GitCloningService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CloneAsync(string gitUrl, string? branch, CancellationToken ct)
    {
        var uri = new Uri(gitUrl);
        if (uri.Scheme != "https")
            throw new InvalidSubmissionException("Only HTTPS git URLs are accepted.");
        if (uri.Host is "localhost" or "127.0.0.1" or "::1")
            throw new InvalidSubmissionException("Local repository URLs are not allowed.");

        var tempPath = Path.Combine(_options.TempDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        _logger.LogInformation("Cloning {GitUrl} (branch: {Branch}) to {Path}", gitUrl, branch ?? "default", tempPath);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.CloneTimeoutSeconds));

        // Use git CLI for cloning (LibGit2Sharp alternative)
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone --depth 1 --single-branch {(branch != null ? $"--branch {branch}" : "")} {gitUrl} {tempPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync(cts.Token);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cts.Token);
            Directory.Delete(tempPath, recursive: true);
            throw new InvalidSubmissionException($"Git clone failed: {error}");
        }

        // Validate size
        var dirSize = GetDirectorySize(tempPath);
        if (dirSize > _options.MaxRepoSizeBytes)
        {
            Directory.Delete(tempPath, recursive: true);
            throw new InvalidSubmissionException(
                $"Repository size ({dirSize / (1024 * 1024)}MB) exceeds maximum ({_options.MaxRepoSizeBytes / (1024 * 1024)}MB).");
        }

        return tempPath;
    }

    private static long GetDirectorySize(string path)
        => Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
}
