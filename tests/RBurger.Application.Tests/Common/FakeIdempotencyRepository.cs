using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Tests.Common;

// Day 13 addition - simple in-memory fake for IIdempotencyRepository, following the exact same
// dependency-free pattern as every other fake in this test project. TryBeginAsync reproduces
// the real EF implementation's "unique index (Endpoint, ScopeId, Key) rejects a concurrent
// duplicate insert" behavior in plain C#, so IdempotencyBehaviorTests can assert the same
// dedup/conflict semantics without a real database.
internal class FakeIdempotencyRepository : IIdempotencyRepository
{
    public List<IdempotencyRecord> Records { get; } = new();

    public Task<bool> TryBeginAsync(IdempotencyRecord record, CancellationToken cancellationToken)
    {
        var exists = Records.Any(r =>
            r.Endpoint == record.Endpoint && r.ScopeId == record.ScopeId && r.Key == record.Key);

        if (exists)
        {
            return Task.FromResult(false);
        }

        Records.Add(record);
        return Task.FromResult(true);
    }

    public Task<IdempotencyRecord?> FindAsync(
        string endpoint, Guid scopeId, string key, CancellationToken cancellationToken)
    {
        return Task.FromResult(Records.FirstOrDefault(
            r => r.Endpoint == endpoint && r.ScopeId == scopeId && r.Key == key));
    }

    public Task CompleteAsync(Guid id, string responseJson, CancellationToken cancellationToken)
    {
        var record = Records.FirstOrDefault(r => r.Id == id);
        if (record is not null)
        {
            record.Status = "Completed";
            record.ResponseJson = responseJson;
            record.CompletedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task AbandonAsync(Guid id, CancellationToken cancellationToken)
    {
        Records.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }
}
