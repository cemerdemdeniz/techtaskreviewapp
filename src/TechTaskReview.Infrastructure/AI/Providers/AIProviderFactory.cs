using Microsoft.Extensions.Options;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Infrastructure.AI.Providers;

public class AIProviderFactory : IAIProviderFactory
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly AIProviderOptions _options;

    public AIProviderFactory(IEnumerable<IAIProvider> providers, IOptions<AIProviderOptions> options)
    {
        _providers = providers;
        _options = options.Value;
    }

    public IAIProvider GetProvider(string? preferredProvider = null)
    {
        var name = preferredProvider ?? _options.DefaultProvider;
        return _providers.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"AI provider '{name}' not registered.");
    }

    public IAIProvider GetFallbackProvider()
    {
        foreach (var provider in _providers.OrderBy(p => p.Name == _options.DefaultProvider ? 0 : 1))
        {
            if (provider.IsAvailableAsync(CancellationToken.None).GetAwaiter().GetResult())
                return provider;
        }
        throw new InvalidOperationException("No AI provider is available.");
    }
}
