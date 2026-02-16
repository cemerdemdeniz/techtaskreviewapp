using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Aggregates.Reviews;
using TechTaskReview.Domain.Common;
using TechTaskReview.Domain.Events;
using TechTaskReview.Domain.Exceptions;

namespace TechTaskReview.Domain.Aggregates.Submissions;

public class Submission : AggregateRoot
{
    public Guid CandidateId { get; private set; }
    public string StoragePath { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public long FileSizeBytes { get; private set; }
    public SubmissionSource Source { get; private set; }
    public string? GitUrl { get; private set; }
    public string? GitBranch { get; private set; }
    public SubmissionStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public ProjectType? DetectedProjectType { get; private set; }
    public string? ExtractedPath { get; private set; }
    public int? TotalFiles { get; private set; }
    public int? TotalLinesOfCode { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? ProcessingCompletedAt { get; private set; }

    private readonly List<SubmissionFile> _files = [];
    public IReadOnlyList<SubmissionFile> Files => _files.AsReadOnly();

    public CodeReview? Review { get; private set; }

    private Submission() { }

    public static Submission CreateFromUpload(
        Guid candidateId, string storagePath, string fileName, long fileSize, CandidateRole role)
    {
        var submission = new Submission
        {
            CandidateId = candidateId,
            StoragePath = storagePath,
            OriginalFileName = fileName,
            FileSizeBytes = fileSize,
            Source = SubmissionSource.ZipUpload,
            Status = SubmissionStatus.Uploaded
        };

        submission.RaiseDomainEvent(new SubmissionCreatedEvent(submission.Id, candidateId, role));
        return submission;
    }

    public static Submission CreateFromGit(
        Guid candidateId, string gitUrl, string? branch, CandidateRole role)
    {
        var submission = new Submission
        {
            CandidateId = candidateId,
            GitUrl = gitUrl,
            GitBranch = branch ?? "main",
            OriginalFileName = new Uri(gitUrl).Segments.Last().TrimEnd('/'),
            Source = SubmissionSource.GitRepository,
            Status = SubmissionStatus.Uploaded
        };

        submission.RaiseDomainEvent(new SubmissionCreatedEvent(submission.Id, candidateId, role));
        return submission;
    }

    public void MarkExtracting() => TransitionTo(SubmissionStatus.Extracting);
    public void MarkMalwareScanning() => TransitionTo(SubmissionStatus.MalwareScanning);
    public void MarkStaticAnalysis() => TransitionTo(SubmissionStatus.StaticAnalysis);
    public void MarkAIReview() => TransitionTo(SubmissionStatus.AIReview);
    public void MarkScoring() => TransitionTo(SubmissionStatus.Scoring);

    public void MarkCompleted()
    {
        TransitionTo(SubmissionStatus.Completed);
        ProcessingCompletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ReviewCompletedEvent(Id, CandidateId));
    }

    public void MarkFailed(string reason)
    {
        Status = SubmissionStatus.Failed;
        FailureReason = reason;
        ProcessingCompletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ReviewFailedEvent(Id, CandidateId, reason));
    }

    public void MarkQuarantined(string reason)
    {
        Status = SubmissionStatus.Quarantined;
        FailureReason = $"MALWARE DETECTED: {reason}";
    }

    public void SetExtractionResults(string extractedPath, ProjectType projectType, int totalFiles, int totalLoc)
    {
        ExtractedPath = extractedPath;
        DetectedProjectType = projectType;
        TotalFiles = totalFiles;
        TotalLinesOfCode = totalLoc;
    }

    public void AddFile(SubmissionFile file) => _files.Add(file);

    public void ResetForRetry()
    {
        if (Status != SubmissionStatus.Failed)
            throw new DomainException("Only failed submissions can be retried.");

        Status = SubmissionStatus.Uploaded;
        FailureReason = null;
        ProcessingStartedAt = null;
        ProcessingCompletedAt = null;
        UpdatedAt = DateTime.UtcNow;
        _files.Clear();

        RaiseDomainEvent(new SubmissionCreatedEvent(Id, CandidateId, default));
    }

    private void TransitionTo(SubmissionStatus newStatus)
    {
        // Validate state transitions
        var validTransitions = new Dictionary<SubmissionStatus, SubmissionStatus[]>
        {
            [SubmissionStatus.Uploaded] = [SubmissionStatus.Extracting, SubmissionStatus.Failed],
            [SubmissionStatus.Extracting] = [SubmissionStatus.MalwareScanning, SubmissionStatus.Failed],
            [SubmissionStatus.MalwareScanning] = [SubmissionStatus.StaticAnalysis, SubmissionStatus.Quarantined, SubmissionStatus.Failed],
            [SubmissionStatus.StaticAnalysis] = [SubmissionStatus.AIReview, SubmissionStatus.Failed],
            [SubmissionStatus.AIReview] = [SubmissionStatus.Scoring, SubmissionStatus.Failed],
            [SubmissionStatus.Scoring] = [SubmissionStatus.Completed, SubmissionStatus.Failed],
        };

        if (!validTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new DomainException($"Cannot transition from {Status} to {newStatus}.");

        if (Status == SubmissionStatus.Uploaded)
            ProcessingStartedAt = DateTime.UtcNow;

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
