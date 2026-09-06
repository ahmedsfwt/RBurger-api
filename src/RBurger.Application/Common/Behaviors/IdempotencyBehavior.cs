using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Behaviors;

// Day 13 addition. Resolves the previously "presence-only, no dedup" limitation on the three
// documented Idempotency-Key-required endpoints (§5.5): POST /orders, POST
// /payments/{orderId}/charge, POST /admin/menu-items/{id}/image. Requests that don't implement
// IIdempotentRequest pass straight through, so this behavior is a no-op for the rest of the API.
public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IIdempotencyRepository _idempotencyRepository;

    public IdempotencyBehavior(IIdempotencyRepository idempotencyRepository)
    {
        _idempotencyRepository = idempotencyRepository;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IIdempotentRequest idempotent)
        {
            return await next();
        }

        // §5.5's presence requirement is enforced by each command's FluentValidator, which
        // runs earlier in the pipeline (ValidationBehavior is registered first). A null/empty
        // key here would only be reached if that validation were ever bypassed - defensive
        // fallthrough, not a silent skip of a documented rule.
        if (string.IsNullOrWhiteSpace(idempotent.IdempotencyKey))
        {
            return await next();
        }

        var requestHash = ComputeFingerprintHash(idempotent.IdempotencyFingerprint);

        var record = new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Endpoint = idempotent.IdempotencyEndpoint,
            ScopeId = idempotent.IdempotencyScopeId,
            Key = idempotent.IdempotencyKey,
            RequestHash = requestHash,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow
        };

        var created = await _idempotencyRepository.TryBeginAsync(record, cancellationToken);

        if (!created)
        {
            var existing = await _idempotencyRepository.FindAsync(
                idempotent.IdempotencyEndpoint,
                idempotent.IdempotencyScopeId,
                idempotent.IdempotencyKey,
                cancellationToken);

            if (existing is null)
            {
                // Narrow race: the conflicting row was removed (AbandonAsync, after a failed
                // first attempt) between the failed insert above and this read. Safe to treat
                // as a fresh attempt.
                created = await _idempotencyRepository.TryBeginAsync(record, cancellationToken);
                if (!created)
                {
                    throw ConflictInProgress();
                }
            }
            else if (existing.Status == "Completed")
            {
                if (existing.RequestHash != requestHash)
                {
                    // §5.5/Ahmed's Day 13 instruction: "Same key + different request => reject
                    // with the appropriate conflict/error."
                    throw new ConflictException(
                        "This Idempotency-Key was already used to complete a different request.",
                        "IDEMPOTENCY_KEY_REUSED");
                }

                // "Same key + same logical request => return the original result" - replayed
                // without re-invoking the handler, so the underlying write never runs twice.
                return JsonSerializer.Deserialize<TResponse>(existing.ResponseJson!)!;
            }
            else
            {
                // Status == "InProgress": another request with the same key is currently
                // executing. Rejected outright (rather than polled/awaited) so "must not
                // execute the operation twice" holds without introducing cross-request locking.
                throw ConflictInProgress();
            }
        }

        try
        {
            var response = await next();

            await _idempotencyRepository.CompleteAsync(
                record.Id, JsonSerializer.Serialize(response), cancellationToken);

            return response;
        }
        catch
        {
            // Only a successful result is cached for replay. A failed attempt (validation,
            // business-rule, or infrastructure exception) does not consume the key, so the
            // caller can retry with the same Idempotency-Key once the underlying problem is
            // fixed - deliberately not required by Ahmed's Day 13 instruction, but the standard
            // behavior for this kind of key (mirrors Stripe's documented idempotency semantics)
            // and avoids permanently locking a caller out after a transient failure.
            await _idempotencyRepository.AbandonAsync(record.Id, cancellationToken);
            throw;
        }
    }

    private static ConflictException ConflictInProgress() => new(
        "This Idempotency-Key is currently being processed by another request.",
        "IDEMPOTENCY_KEY_IN_PROGRESS");

    private static string ComputeFingerprintHash(string fingerprint)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToHexString(bytes);
    }
}
