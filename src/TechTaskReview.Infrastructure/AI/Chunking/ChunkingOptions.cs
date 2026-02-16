namespace TechTaskReview.Infrastructure.AI.Chunking;

public class ChunkingOptions
{
    public int MaxTokensPerChunk { get; set; } = 3000;
    public int MaxChunksPerSubmission { get; set; } = 50;
    public bool EnableSampling { get; set; } = true;
    public string ExtractedPath { get; set; } = null!;
}
