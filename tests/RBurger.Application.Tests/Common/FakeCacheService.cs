using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Tests.Common;

// Day 15 addition - simple in-memory fake for ICacheService, dependency-free per this test
// project's established convention.
internal class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        if (_store.TryGetValue(key, out var value))
        {
            return Task.FromResult((T?)value);
        }

        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }
}
