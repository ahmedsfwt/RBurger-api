using Microsoft.Extensions.Caching.Memory;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Infrastructure.Caching;

// Day 15 addition (Backend Parity Spec §12) - see ICacheService's XML comment for why this is
// in-process IMemoryCache rather than Redis.
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken)
    {
        _cache.Set(key, value, duration);
        return Task.CompletedTask;
    }
}
