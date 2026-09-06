namespace RBurger.Application.Common.Interfaces;

// Implemented by the three, and only the three, documented Idempotency-Key-required commands
// (§5.5): CreateOrderCommand, ChargePaymentCommand, UploadMenuItemImageCommand.
// IdempotencyBehavior (Common/Behaviors) wraps any MediatR request implementing this and
// persists/dedupes it by (IdempotencyEndpoint, IdempotencyScopeId, IdempotencyKey).
public interface IIdempotentRequest
{
    // Populated by the controller from the "Idempotency-Key" header, exactly as today -
    // presence is still enforced by each command's FluentValidator (§5.5).
    string? IdempotencyKey { get; }

    // A short, fixed literal identifying which of the three documented endpoints this is -
    // NOT part of any JSON contract, purely an internal uniqueness-scoping key so the same
    // Idempotency-Key value used against two different endpoints by the same caller can never
    // collide.
    string IdempotencyEndpoint { get; }

    // The calling Customer's or Admin's id - scopes the key per caller, so "one user's key
    // cannot incorrectly affect another user's request" (Ahmed's Day 13 instruction).
    Guid IdempotencyScopeId { get; }

    // A stable string representation of every field that defines "the same logical request"
    // for this specific command, deliberately hand-written per command rather than a generic
    // JSON-serialize-the-whole-object approach - UploadMenuItemImageCommand carries a raw
    // file Stream that System.Text.Json cannot (and should not, for a multi-megabyte upload)
    // serialize on every request; its fingerprint uses content-type/length as a proxy instead.
    string IdempotencyFingerprint { get; }
}
