using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.AI.Chunking;

public enum FilePriority { Core, Test, Config }

public record FileForReview(SubmissionFile File, FilePriority Priority);

public class ReviewChunk
{
    public List<FileForReview> Files { get; set; } = [];
    public string CombinedContent { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public int EstimatedTokens { get; set; }
}
