namespace TechTaskReview.Application.Common.Interfaces;

public record AIReviewRequest(string Prompt, double Temperature, int MaxResponseTokens);
public record AIReviewResponse(string Content, long DurationMs);

public interface IAIProvider
{
    string Name { get; }
    int MaxTokens { get; }
    Task<AIReviewResponse> ReviewCodeAsync(AIReviewRequest request, CancellationToken ct);
    Task<bool> IsAvailableAsync(CancellationToken ct);
}

public interface IAIProviderFactory
{
    IAIProvider GetProvider(string? preferredProvider = null);
    IAIProvider GetFallbackProvider();
}
