namespace TechTaskReview.Application.Submissions.Queries.GetSubmission;

public record SubmissionDetailDto(
    Guid Id,
    Guid CandidateId,
    string CandidateName,
    string Source,
    string OriginalFileName,
    long FileSizeBytes,
    string Status,
    string? FailureReason,
    ProjectTypeDto? DetectedProject,
    int? TotalFiles,
    int? TotalLinesOfCode,
    DateTime? ProcessingStartedAt,
    DateTime? ProcessingCompletedAt,
    Guid? ReviewId,
    DateTime CreatedAt);

public record ProjectTypeDto(string PrimaryLanguage, string? Framework, string[] BuildFiles, bool IsMonorepo);
