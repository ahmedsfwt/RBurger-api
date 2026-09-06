namespace RBurger.Application.Common.Interfaces;

// Day 15 addition (Backend Parity Spec §12): §13.1 requires "Admin analytics endpoints ...
// cached server-side for 60 s to protect the primary database from dashboard polling." No
// cache infrastructure (Redis or otherwise) previously existed in this project, and per the
// spec's explicit instruction, Redis is not introduced solely for this - this abstraction is
// the smallest architecture-compatible approach, implemented by an in-process IMemoryCache in
// Infrastructure (MemoryCacheService). Swapping to a distributed cache (e.g. the ElastiCache
// Redis backplane already used for SignalR, §10.1) later only requires a new implementation of
// this same interface - no Application-layer code would change.
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);

    Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken);
}
