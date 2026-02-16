using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Infrastructure.AI.Providers;

public class OllamaProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly AIProviderOptions _options;
    private readonly ILogger<OllamaProvider> _logger;

    public string Name => "Ollama";
    public int MaxTokens => 4096;

    public OllamaProvider(HttpClient httpClient, IOptions<AIProviderOptions> options, ILogger<OllamaProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_options.OllamaBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<AIReviewResponse> ReviewCodeAsync(AIReviewRequest request, CancellationToken ct)
    {
        var payload = new
        {
            model = _options.OllamaModel,
            prompt = request.Prompt,
            stream = false,
            options = new
            {
                temperature = request.Temperature,
                num_predict = request.MaxResponseTokens,
                top_p = 0.9,
                repeat_penalty = 1.1
            },
            format = "json"
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await _httpClient.PostAsJsonAsync("/api/generate", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        sw.Stop();

        var content = result.GetProperty("response").GetString() ?? "";
        _logger.LogInformation("Ollama response in {Ms}ms, {Len} chars", sw.ElapsedMilliseconds, content.Length);

        return new AIReviewResponse(content, sw.ElapsedMilliseconds);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
