namespace RBurger.Domain.Entities;

// New table, added under Ahmed's explicit Day 13 approval to modify the §6.2 schema.
// Previously blocked: §5.5 requires an Idempotency-Key header on three POST endpoints, but
// §6 documents no table to persist idempotency state in, so the header was only ever
// validated for presence (FluentValidation NotEmpty) - never actually deduplicated,
// meaning a client retry with the same key could execute the underlying write twice.
//
// One row represents one (Endpoint, ScopeId, Key) attempt. ScopeId is the calling
// Customer/Admin's id (never null for the three documented endpoints, all of which require a
// JWT), so one caller's key can never collide with another caller's key for the same endpoint.
public class IdempotencyRecord
{
    public Guid Id { get; set; }

    // The three literal, documented endpoints this applies to (§5.5): "orders:create",
    // "payments:charge", "menu-items:upload-image". Included in the uniqueness scope so a key
    // reused across two different documented endpoints by the same caller cannot collide.
    public string Endpoint { get; set; } = string.Empty;

    // The calling Customer/Admin's id (sub claim) - scopes the key per caller.
    public Guid ScopeId { get; set; }

    public string Key { get; set; } = string.Empty;

    // SHA-256 hex digest of the logical request payload (everything except the idempotency
    // key itself), used to detect "same key + different request" (§5.5/Ahmed's Day 13
    // instruction: "reject with the appropriate conflict/error").
    public string RequestHash { get; set; } = string.Empty;

    // "InProgress" while the wrapped handler is executing (lets a concurrent duplicate request
    // be rejected instead of re-running the operation - "Concurrent requests using the same
    // key must not execute the operation twice"); "Completed" once a result has been recorded.
    public string Status { get; set; } = string.Empty;

    // The wrapped handler's response, JSON-serialized, so a repeat call with the same key +
    // same request can be answered with the original result instead of re-executing.
    // Null while Status == InProgress.
    public string? ResponseJson { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
