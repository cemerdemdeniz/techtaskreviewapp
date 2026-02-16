namespace TechTaskReview.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct);
    Task InvalidateAsync(string keyPattern, CancellationToken ct);
}
