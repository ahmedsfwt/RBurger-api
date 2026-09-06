using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Interfaces;

// Day 13 addition (approved schema change) - resolves the Day 3 blocker recorded on
// IJwtTokenGenerator.GenerateOpaqueRefreshToken: refresh tokens are now persisted
// and validated by POST /api/v1/auth/refresh (RefreshTokenCommandHandler).
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    // Mirrors IOrderRepository.TryClaimForDriverAsync's conditional-UPDATE pattern exactly:
    // atomically revokes this token ONLY if it is still active (RevokedAt IS NULL), so two
    // concurrent /refresh calls presenting the same raw token cannot both succeed - the loser
    // observes RowsAffected == 0 and is treated as a reuse attempt.
    Task<bool> TryRevokeAsync(Guid tokenId, Guid replacedByTokenId, CancellationToken cancellationToken);

    // Reuse-detection defense: presenting an already-revoked/rotated token again is treated as
    // a signal the token may have been stolen, so every other still-active token for the same
    // identity is revoked too, forcing re-authentication everywhere.
    Task RevokeAllActiveForUserAsync(string userType, Guid userId, CancellationToken cancellationToken);

    // Day 14 addition (Backend Parity Spec §5 - logout): plain, unconditional revoke with no
    // replacement token, unlike TryRevokeAsync which is specifically the rotation primitive
    // (always pairs a revoke with a successor token). Idempotent - revoking an already-revoked
    // token is a no-op, matching logout's documented idempotent behavior.
    Task RevokeAsync(Guid tokenId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
