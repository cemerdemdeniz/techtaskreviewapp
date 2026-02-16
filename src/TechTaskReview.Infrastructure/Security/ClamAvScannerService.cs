using Microsoft.Extensions.Logging;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Infrastructure.Security;

public class ClamAvScannerService : IMalwareScannerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClamAvScannerService> _logger;

    public ClamAvScannerService(HttpClient httpClient, ILogger<ClamAvScannerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MalwareScanResult> ScanDirectoryAsync(string directoryPath, CancellationToken ct)
    {
        var findings = new List<string>();
        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
        var semaphore = new SemaphoreSlim(10);

        _logger.LogInformation("Scanning {FileCount} files in {Path}", files.Length, directoryPath);

        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await using var stream = File.OpenRead(file);
                using var content = new StreamContent(stream);
                var response = await _httpClient.PostAsync("/scan", content, ct);
                var result = await response.Content.ReadAsStringAsync(ct);

                if (result.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
                {
                    lock (findings)
                    {
                        findings.Add($"{Path.GetFileName(file)}: {result.Trim()}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan file {File}", file);
            }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Scan complete. {FindingsCount} findings.", findings.Count);
        return new MalwareScanResult(findings.Count == 0, findings);
    }
}
