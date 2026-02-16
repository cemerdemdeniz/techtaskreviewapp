namespace TechTaskReview.Domain.Aggregates.Submissions;

public enum SubmissionStatus
{
    Uploaded = 1,
    Extracting = 2,
    MalwareScanning = 3,
    StaticAnalysis = 4,
    AIReview = 5,
    Scoring = 6,
    Completed = 7,
    Failed = 8,
    Quarantined = 9   // Malware detected
}
