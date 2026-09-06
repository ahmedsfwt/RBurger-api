using MediatR;
using RBurger.Application.Authentication.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Security;
using RBurger.Domain.Entities;
// Day 10 fix - see IAdminRepository.cs's comment for the full explanation of why this alias
// is required (RBurger.Application.Admin namespace shadows the bare "Admin" type name).
using AdminEntity = RBurger.Domain.Entities.Admin;
// Day 13 fix, same class of bug: this file's own namespace
// (RBurger.Application.Authentication.Commands.RefreshToken) shadows the bare "RefreshToken"
// entity type name for every type declared inside it - see the identical fix applied to the
// four login/signup handlers (CustomerSignup/CustomerLogin/DriverLogin/AdminLogin) for the
// full explanation.
using RefreshTokenEntity = RBurger.Domain.Entities.RefreshToken;

namespace RBurger.Application.Authentication.Commands.RefreshToken;

// §7.1 POST /api/v1/auth/refresh. Day 13 addition - resolves the Day 3 blocker: refresh tokens
// are now persisted (RefreshToken.cs) and this is the first and only endpoint that consumes
// them.
//
// Rotation flow: validate hash -> check expiration -> check revoked/reuse -> atomically revoke
// the presented token (IRefreshTokenRepository.TryRevokeAsync, guarding concurrent double-use
// exactly like IOrderRepository.TryClaimForDriverAsync) -> issue a new access token + persist a
// brand-new RefreshToken row chained via ReplacedByTokenId.
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ICustomerRepository customerRepository,
        IDriverRepository driverRepository,
        IAdminRepository adminRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _customerRepository = customerRepository;
        _driverRepository = driverRepository;
        _adminRepository = adminRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenHasher.Hash(request.RefreshToken);

        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existingToken is null)
        {
            throw new InvalidCredentialsException("Invalid or unknown refresh token.");
        }

        if (existingToken.RevokedAt is not null)
        {
            // Reuse of an already-rotated/revoked token - standard refresh-token-theft-
            // detection response: treat the whole token family as compromised and force
            // re-authentication everywhere for this identity, not just reject this one call.
            await _refreshTokenRepository.RevokeAllActiveForUserAsync(
                existingToken.UserType, existingToken.UserId, cancellationToken);
            throw new InvalidCredentialsException("This refresh token has already been used.");
        }

        if (existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidCredentialsException("This refresh token has expired.");
        }

        // Re-verify the underlying identity still exists and, for Driver/Admin, is still
        // active - mirrors §7.6.3's "a disabled driver's login is rejected with 403 even with
        // the correct password" rule extended to the refresh flow, so a disabled account
        // cannot keep obtaining fresh access tokens indefinitely via a refresh token issued
        // before it was disabled.
        var accessToken = existingToken.UserType switch
        {
            "Customer" => _jwtTokenGenerator.GenerateAccessToken(
                await _customerRepository.GetByIdAsync(existingToken.UserId, cancellationToken)
                    ?? throw new InvalidCredentialsException("Invalid or unknown refresh token.")),

            "Driver" => _jwtTokenGenerator.GenerateAccessToken(await GetActiveDriverAsync(existingToken.UserId, cancellationToken)),

            "Admin" => _jwtTokenGenerator.GenerateAccessToken(await GetActiveAdminAsync(existingToken.UserId, cancellationToken)),

            _ => throw new InvalidCredentialsException("Invalid or unknown refresh token.")
        };

        var newRefresh = _jwtTokenGenerator.GenerateOpaqueRefreshToken();
        var newTokenId = Guid.NewGuid();

        // Atomic conditional revoke (mirrors IOrderRepository.TryClaimForDriverAsync): only
        // succeeds if RevokedAt was still NULL at the moment of the UPDATE. A concurrent
        // second /refresh call presenting the exact same raw token loses this race and is
        // rejected as a reuse attempt, so the old token can never be used to mint two
        // successors.
        var revoked = await _refreshTokenRepository.TryRevokeAsync(existingToken.Id, newTokenId, cancellationToken);
        if (!revoked)
        {
            throw new InvalidCredentialsException("This refresh token has already been used.");
        }

        await _refreshTokenRepository.AddAsync(
            new RefreshTokenEntity
            {
                Id = newTokenId,
                TokenHash = RefreshTokenHasher.Hash(newRefresh.RawToken),
                UserType = existingToken.UserType,
                UserId = existingToken.UserId,
                ExpiresAt = newRefresh.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // §7.1 response shape - Day 16: now includes the replacement refreshToken (approved
        // contract change, see RefreshTokenResponse's XML comment).
        return new RefreshTokenResponse
        {
            AccessToken = accessToken.AccessToken,
            RefreshToken = newRefresh.RawToken,
            ExpiresInSeconds = accessToken.ExpiresInSeconds
        };
    }

    private async Task<Driver> GetActiveDriverAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken)
            ?? throw new InvalidCredentialsException("Invalid or unknown refresh token.");

        if (!driver.IsActive)
        {
            throw new ForbiddenException("This driver account has been disabled.");
        }

        return driver;
    }

    private async Task<AdminEntity> GetActiveAdminAsync(Guid adminId, CancellationToken cancellationToken)
    {
        var admin = await _adminRepository.GetByIdAsync(adminId, cancellationToken)
            ?? throw new InvalidCredentialsException("Invalid or unknown refresh token.");

        if (!admin.IsActive)
        {
            throw new ForbiddenException("This admin account has been disabled.");
        }

        return admin;
    }
}
