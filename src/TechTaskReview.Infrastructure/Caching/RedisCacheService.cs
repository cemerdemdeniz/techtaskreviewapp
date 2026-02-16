using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct)
    {
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<T>(cached);

        var value = await factory();
        if (value is not null)
        {
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        return value;
    }

    public async Task InvalidateAsync(string keyPattern, CancellationToken ct)
    {
        // Simple single-key removal; pattern-based requires Redis scripting
        await _cache.RemoveAsync(keyPattern, ct);
    }
}
