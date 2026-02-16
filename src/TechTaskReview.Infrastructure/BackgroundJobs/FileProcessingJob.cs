using System.IO.Compression;
using System.Text.Json;
using Hangfire;
using Microsoft.Extensions.Logging;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Submissions.EventHandlers;
using TechTaskReview.Domain.Aggregates.Submissions;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Infrastructure.BackgroundJobs;

public class FileProcessingJob : IFileProcessingJob
{
    private readonly ISubmissionRepository _submissions;
    private readonly IFileStorageService _fileStorage;
    private readonly IGitCloningService _gitCloning;
    private readonly IMalwareScannerService _malwareScanner;
    private readonly IStaticAnalysisService _staticAnalysis;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _jobClient;
    private readonly ILogger<FileProcessingJob> _logger;

    public FileProcessingJob(
        ISubmissionRepository submissions, IFileStorageService fileStorage,
        IGitCloningService gitCloning, IMalwareScannerService malwareScanner,
        IStaticAnalysisService staticAnalysis, IUnitOfWork unitOfWork,
        IBackgroundJobClient jobClient, ILogger<FileProcessingJob> logger)
    {
        _submissions = submissions; _fileStorage = fileStorage;
        _gitCloning = gitCloning; _malwareScanner = malwareScanner;
        _staticAnalysis = staticAnalysis; _unitOfWork = unitOfWork;
        _jobClient = jobClient; _logger = logger;
    }

    [Queue("default")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 600 })]
    public async Task ExecuteAsync(Guid submissionId, CancellationToken ct)
    {
        var submission = await _submissions.GetByIdAsync(submissionId, ct)
            ?? throw new InvalidOperationException($"Submission {submissionId} not found.");

        if (submission.Status != SubmissionStatus.Uploaded) return; // Idempotency

        try
        {
            // Stage 2: Extract
            submission.MarkExtracting();
            await _unitOfWork.SaveChangesAsync(ct);

            var extractedPath = submission.Source == SubmissionSource.GitRepository
                ? await _gitCloning.CloneAsync(submission.GitUrl!, submission.GitBranch, ct)
                : await ExtractArchive(submission, ct);

            var detector = new ProjectTypeDetector();
            var projectType = detector.Detect(extractedPath);
            var inventory = new FileInventoryService();
            var files = inventory.BuildInventory(submission.Id, extractedPath);

            submission.SetExtractionResults(extractedPath, projectType, files.Count,
                files.Sum(f => f.LineCount));
            foreach (var file in files) submission.AddFile(file);
            await _unitOfWork.SaveChangesAsync(ct);

            // Stage 3: Malware Scan
            submission.MarkMalwareScanning();
            await _unitOfWork.SaveChangesAsync(ct);

            var scanResult = await _malwareScanner.ScanDirectoryAsync(extractedPath, ct);
            if (!scanResult.IsClean)
            {
                submission.MarkQuarantined(string.Join("; ", scanResult.Findings));
                await _unitOfWork.SaveChangesAsync(ct);
                return;
            }

            // Stage 4: Static Analysis
            submission.MarkStaticAnalysis();
            await _unitOfWork.SaveChangesAsync(ct);

            var analysisResult = await _staticAnalysis.AnalyzeAsync(extractedPath, projectType, ct);
            var analysisJson = JsonSerializer.Serialize(analysisResult);

            // Enqueue AI Review
            submission.MarkAIReview();
            await _unitOfWork.SaveChangesAsync(ct);

            _jobClient.Enqueue<AIReviewJob>(
                job => job.ExecuteAsync(submissionId, analysisJson, CancellationToken.None));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File processing failed for submission {Id}", submissionId);
            submission.MarkFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(ct);
            throw;
        }
    }

    private async Task<string> ExtractArchive(Submission submission, CancellationToken ct)
    {
        var archiveStream = await _fileStorage.RetrieveAsync(submission.StoragePath, ct);
        var tempDir = Path.Combine(Path.GetTempPath(), "techtask-extract", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var tempArchive = Path.Combine(tempDir, submission.OriginalFileName);
        await using (var fs = File.Create(tempArchive))
            await archiveStream.CopyToAsync(fs, ct);

        var extractDir = Path.Combine(tempDir, "extracted");
        ZipFile.ExtractToDirectory(tempArchive, extractDir);
        File.Delete(tempArchive);

        return extractDir;
    }
}
