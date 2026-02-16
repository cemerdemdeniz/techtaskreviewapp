namespace TechTaskReview.Infrastructure.AI;

public class AIProviderOptions
{
    public string DefaultProvider { get; set; } = "Ollama";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "deepseek-coder:6.7b";
    public string? OpenAIApiKey { get; set; }
    public string OpenAIModel { get; set; } = "gpt-4o-mini";
    public int MaxConcurrentRequests { get; set; } = 3;
    public double Temperature { get; set; } = 0.1;
    public int MaxResponseTokens { get; set; } = 2048;
    public int MaxChunksPerSubmission { get; set; } = 50;
}
