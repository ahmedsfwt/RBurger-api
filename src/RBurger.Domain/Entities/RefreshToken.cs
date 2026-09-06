namespace RBurger.Domain.Entities;

// New table, added under Ahmed's explicit Day 13 approval to modify the §6.2 schema.
// Previously blocked (Day 3 report): §6 documents no RefreshTokens table, so refresh tokens
// were never persisted - only an opaque, non-validated placeholder was returned to satisfy
// §7.1's AuthResponse shape.
//
// Polymorphic across the three identity tables (Customers/Drivers/Admins), mirroring the
// exact pattern already established by OrderStatusEvent.ActorId: "DriverId or CustomerId,
// nullable for system - polymorphic actor reference, no single-table FK is documented, so no
// navigation property is added for it." UserType/UserId here play the same role for a subject
// that can be any of the three identity tables, so no FK/navigation property is added.
public class RefreshToken
{
    public Guid Id { get; set; }

    // SHA-256 hex digest of the raw opaque token handed to the client. The raw value is never
    // persisted (approved decision: "Never store raw refresh tokens if the architecture can
    // safely use hashes" - a refresh token is already high-entropy random data, so a fast,
    // deterministic, unsalted hash is appropriate here, unlike password hashing).
    public string TokenHash { get; set; } = string.Empty;

    // Closed set mirroring the JWT "role" claim's three literal values (§5.4): Customer,
    // Driver, Admin. Kept as a plain string (not a shared enum with OrderStage, etc.) since
    // there is no existing "Role" enum in the Domain layer to reuse without inventing one.
    public string UserType { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Null while active. Set the moment this token is rotated (successfully used to obtain a
    // new one) or revoked (reuse-detection response - see RefreshTokenCommandHandler).
    public DateTime? RevokedAt { get; set; }

    // Set at the same time as RevokedAt, when rotation (not a reuse-detection revocation)
    // produced a successor token - lets a reuse of this now-revoked token be distinguished
    // from an ordinary "expired/never revoked" rejection in an audit read, without adding a
    // dedicated reason column that no test/consumer currently needs.
    public Guid? ReplacedByTokenId { get; set; }
}
