using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Infrastructure.AI.Providers;

public class OpenAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly AIProviderOptions _options;
    private readonly ILogger<OpenAIProvider> _logger;

    public string Name => "OpenAI";
    public int MaxTokens => 128000;

    public OpenAIProvider(HttpClient httpClient, IOptions<AIProviderOptions> options, ILogger<OpenAIProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        if (!string.IsNullOrEmpty(_options.OpenAIApiKey))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAIApiKey);
    }

    public async Task<AIReviewResponse> ReviewCodeAsync(AIReviewRequest request, CancellationToken ct)
    {
        var payload = new
        {
            model = _options.OpenAIModel,
            messages = new[]
            {
                new { role = "system", content = "You are a senior code reviewer. Respond only in valid JSON." },
                new { role = "user", content = request.Prompt }
            },
            temperature = request.Temperature,
            max_tokens = request.MaxResponseTokens,
            response_format = new { type = "json_object" }
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        sw.Stop();

        var content = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        return new AIReviewResponse(content, sw.ElapsedMilliseconds);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_options.OpenAIApiKey)) return false;
        try
        {
            var response = await _httpClient.GetAsync("v1/models", ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
