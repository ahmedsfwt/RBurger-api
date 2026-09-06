using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Repositories;

// Day 13 addition (approved schema change) - implements IRefreshTokenRepository against
// ApplicationDbContext, following the exact same repository conventions as every other
// repository in this folder (explicit AddAsync/SaveChangesAsync split, ExecuteUpdateAsync for
// conditional/concurrency-safe updates - mirrors OrderRepository.TryClaimForDriverAsync).
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        _context.RefreshTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    // Mirrors OrderRepository.TryClaimForDriverAsync's conditional-UPDATE pattern exactly:
    // a single guarded UPDATE (WHERE RevokedAt IS NULL) so two concurrent /refresh calls
    // presenting the same raw token cannot both succeed - the loser observes RowsAffected == 0.
    public async Task<bool> TryRevokeAsync(Guid tokenId, Guid replacedByTokenId, CancellationToken cancellationToken)
    {
        var rowsAffected = await _context.RefreshTokens
            .Where(t => t.Id == tokenId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, DateTime.UtcNow)
                    .SetProperty(t => t.ReplacedByTokenId, replacedByTokenId),
                cancellationToken);

        return rowsAffected > 0;
    }

    public async Task RevokeAllActiveForUserAsync(string userType, Guid userId, CancellationToken cancellationToken)
    {
        await _context.RefreshTokens
            .Where(t => t.UserType == userType && t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.RevokedAt, DateTime.UtcNow),
                cancellationToken);
    }

    // Day 14 addition (Backend Parity Spec §5 - logout). Idempotent: the WHERE RevokedAt IS
    // NULL guard means calling this twice for the same token is a no-op the second time.
    public async Task RevokeAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        await _context.RefreshTokens
            .Where(t => t.Id == tokenId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.RevokedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
