using Hangfire;
using Microsoft.Extensions.Logging;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Infrastructure.BackgroundJobs;

public class CleanupJob
{
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<CleanupJob> _logger;

    public CleanupJob(IFileStorageService fileStorage, ILogger<CleanupJob> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    [Queue("export")]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Running daily cleanup job");

        // Clean extracted repos older than 30 days
        var extractDir = Path.Combine(Path.GetTempPath(), "techtask-extract");
        if (Directory.Exists(extractDir))
        {
            foreach (var dir in Directory.GetDirectories(extractDir))
            {
                var created = Directory.GetCreationTimeUtc(dir);
                if (created < DateTime.UtcNow.AddDays(-30))
                {
                    Directory.Delete(dir, recursive: true);
                    _logger.LogInformation("Deleted expired extraction directory: {Dir}", dir);
                }
            }
        }

        _logger.LogInformation("Cleanup job completed");
    }
}
