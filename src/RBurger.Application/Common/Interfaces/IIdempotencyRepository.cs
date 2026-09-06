using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Interfaces;

// Day 13 addition (approved schema change), consumed by IdempotencyBehavior
// (Common/Behaviors) - resolves the previously "presence-only, no dedup" limitation flagged
// on CreateOrderCommandValidator/ChargePaymentCommandValidator/
// UploadMenuItemImageCommandValidator's Idempotency-Key rules.
public interface IIdempotencyRepository
{
    // Attempts to insert a new "InProgress" record. Returns false if a record already exists
    // for this (Endpoint, ScopeId, Key) triple - the real EF Core implementation relies on the
    // DB's unique index (IdempotencyRecordConfiguration) as the concurrency safety net, exactly
    // mirroring IOrderRepository.AddWithGeneratedOrderNumberAsync's
    // catch-DbUpdateException-and-report-false pattern.
    Task<bool> TryBeginAsync(IdempotencyRecord record, CancellationToken cancellationToken);

    Task<IdempotencyRecord?> FindAsync(
        string endpoint, Guid scopeId, string key, CancellationToken cancellationToken);

    Task CompleteAsync(Guid id, string responseJson, CancellationToken cancellationToken);

    // Deletes an "InProgress" record after the wrapped handler throws, so a caller can retry
    // the same Idempotency-Key after a transient/business failure instead of being locked out
    // permanently by a failed first attempt.
    Task AbandonAsync(Guid id, CancellationToken cancellationToken);
}
