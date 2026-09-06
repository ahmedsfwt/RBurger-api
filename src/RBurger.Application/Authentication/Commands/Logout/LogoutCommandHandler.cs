using MediatR;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Security;

namespace RBurger.Application.Authentication.Commands.Logout;

// Day 14 addition (Backend Parity Spec §5) - see LogoutCommand's XML comment for the request-
// shape decision. Behavior is deliberately idempotent/safe for every input (per the spec's
// explicit "logout should be safe/idempotent where appropriate"): an unknown or already-revoked
// token is a no-op success rather than an error, since the caller's actual goal - "this token
// must not work anymore" - is already true in both cases. This also avoids leaking, via a 401
// vs 204 distinction, whether a given opaque token value was ever valid.
public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenHasher.Hash(request.RefreshToken);

        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existingToken is null)
        {
            return; // unknown token - already "logged out" from the caller's perspective
        }

        // Do NOT delete the row (Order.CancelledAt-style historical-record precedent applies
        // here too - §6.3-adjacent decisions in this project consistently keep audit rows
        // rather than deleting them). RevokeAsync is a no-op if already revoked, so a repeated
        // logout call is safe.
        await _refreshTokenRepository.RevokeAsync(existingToken.Id, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
