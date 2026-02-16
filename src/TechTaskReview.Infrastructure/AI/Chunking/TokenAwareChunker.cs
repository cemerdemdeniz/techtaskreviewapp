using System.Text.RegularExpressions;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.AI.Chunking;

public interface IFileChunker
{
    List<ReviewChunk> CreateChunks(List<FileForReview> files, string extractedPath);
}

public class TokenAwareChunker : IFileChunker
{
    private readonly ChunkingOptions _options;
    private const double TokensPerChar = 0.285;

    public TokenAwareChunker(ChunkingOptions options) => _options = options;

    public List<ReviewChunk> CreateChunks(List<FileForReview> files, string extractedPath)
    {
        var chunks = new List<ReviewChunk>();
        var currentFiles = new List<(FileForReview file, string content)>();
        int currentTokens = 0;

        var ordered = files.OrderBy(f => f.Priority).ThenByDescending(f => f.File.LineCount);

        foreach (var file in ordered)
        {
            var filePath = Path.Combine(extractedPath, file.File.RelativePath);
            if (!File.Exists(filePath)) continue;

            var content = File.ReadAllText(filePath);
            var estimatedTokens = (int)(content.Length * TokensPerChar);

            if (estimatedTokens > _options.MaxTokensPerChunk)
            {
                // Flush current chunk
                if (currentFiles.Count > 0)
                {
                    chunks.Add(BuildChunk(currentFiles, currentTokens));
                    currentFiles = [];
                    currentTokens = 0;
                }

                // Split large file
                var parts = SplitLargeContent(content, file.File.Language);
                foreach (var part in parts)
                {
                    var partTokens = (int)(part.Length * TokensPerChar);
                    chunks.Add(new ReviewChunk
                    {
                        Files = [file],
                        CombinedContent = $"// {file.File.RelativePath} (partial)\n{part}",
                        EstimatedTokens = partTokens
                    });
                }
                continue;
            }

            if (currentTokens + estimatedTokens > _options.MaxTokensPerChunk)
            {
                chunks.Add(BuildChunk(currentFiles, currentTokens));
                currentFiles = [];
                currentTokens = 0;
            }

            currentFiles.Add((file, content));
            currentTokens += estimatedTokens;
        }

        if (currentFiles.Count > 0)
            chunks.Add(BuildChunk(currentFiles, currentTokens));

        // Trim to max chunks
        if (chunks.Count > _options.MaxChunksPerSubmission && _options.EnableSampling)
            chunks = SampleChunks(chunks, _options.MaxChunksPerSubmission);

        // Set indexes
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].ChunkIndex = i;
            chunks[i].TotalChunks = chunks.Count;
        }

        return chunks;
    }

    private static ReviewChunk BuildChunk(List<(FileForReview file, string content)> files, int tokens)
    {
        var combined = string.Join("\n\n", files.Select(f =>
            $"// === {f.file.File.RelativePath} ===\n{f.content}"));

        return new ReviewChunk
        {
            Files = files.Select(f => f.file).ToList(),
            CombinedContent = combined,
            EstimatedTokens = tokens
        };
    }

    private static List<string> SplitLargeContent(string content, string language)
    {
        var pattern = language switch
        {
            "C#" => @"(?=\n\s*(?:public|private|protected|internal|namespace|class|interface|record|enum)\s)",
            "TypeScript" or "JavaScript" => @"(?=\n\s*(?:export|function|class|interface|type|const\s+\w+\s*=\s*(?:\(|function)))",
            _ => @"\n\n+"
        };

        return Regex.Split(content, pattern)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static List<ReviewChunk> SampleChunks(List<ReviewChunk> chunks, int maxChunks)
    {
        // Always keep first (entry points) and last chunks
        var sampled = new List<ReviewChunk> { chunks[0] };
        var step = (double)(chunks.Count - 1) / (maxChunks - 1);

        for (int i = 1; i < maxChunks - 1; i++)
        {
            var idx = (int)(i * step);
            if (idx < chunks.Count && !sampled.Contains(chunks[idx]))
                sampled.Add(chunks[idx]);
        }

        sampled.Add(chunks[^1]);
        return sampled.DistinctBy(c => c.ChunkIndex).ToList();
    }
}
