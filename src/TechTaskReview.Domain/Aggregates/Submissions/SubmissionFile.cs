using TechTaskReview.Domain.Common;

namespace TechTaskReview.Domain.Aggregates.Submissions;

public class SubmissionFile : Entity
{
    public Guid SubmissionId { get; private set; }
    public string RelativePath { get; private set; } = null!;
    public string Language { get; private set; } = null!;
    public int LineCount { get; private set; }
    public long SizeBytes { get; private set; }
    public bool IsTestFile { get; private set; }
    public bool IsConfigFile { get; private set; }

    private SubmissionFile() { }

    public static SubmissionFile Create(
        Guid submissionId, string relativePath, string language,
        int lineCount, long sizeBytes, bool isTestFile, bool isConfigFile)
    {
        return new SubmissionFile
        {
            SubmissionId = submissionId,
            RelativePath = relativePath,
            Language = language,
            LineCount = lineCount,
            SizeBytes = sizeBytes,
            IsTestFile = isTestFile,
            IsConfigFile = isConfigFile
        };
    }
}
