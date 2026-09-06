using MediatR;
using RBurger.Application.Authentication.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Security;
using RBurger.Domain.Entities;
// Day 13 fix: RBurger.Application.Authentication.Commands.RefreshToken (the refresh-token
// feature namespace added this day) is a nested namespace directly under
// RBurger.Application.Authentication.Commands - the same enclosing namespace this file's
// own feature namespace lives under. C# simple-name lookup checks enclosing namespaces for
// a matching nested namespace/type BEFORE consulting using directives, so bare
// "RefreshToken" here would bind to that namespace, not the Domain.Entities.RefreshToken
// entity, producing CS0118 - the exact same class of bug fixed for "Admin" on Day 10
// (see IAdminRepository.cs). Aliased for the same reason.
using RefreshTokenEntity = RBurger.Domain.Entities.RefreshToken;

namespace RBurger.Application.Authentication.Commands.AdminLogin;

public class AdminLoginCommandHandler : IRequestHandler<AdminLoginCommand, AdminAuthResponse>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AdminLoginCommandHandler(
        IAdminRepository adminRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _adminRepository = adminRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AdminAuthResponse> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        // §7.6.0: "Authenticate an existing Admin by username + password."
        var admin = await _adminRepository.GetByUsernameAsync(request.Username, cancellationToken);

        // Mirrors DriverLoginCommandHandler's check order exactly (approved Day 6 decision,
        // reused per Ahmed's "follow existing authentication conventions" instruction for
        // Day 10): credentials checked first, regardless of IsActive, so a disabled account
        // with a wrong password still gets 401 (INVALID_CREDENTIALS), never leaking a
        // disabled-account signal to a caller who doesn't even have the right password.
        if (admin is null || !_passwordHasher.Verify(admin.PasswordHash, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        // No §7.6.x statement documents disabled-Admin-login behavior as explicitly as §7.6.3
        // does for a disabled Driver, but Ahmed's Day 10 instruction is to follow the existing
        // convention consistently - reuses the same generic ForbiddenException/"FORBIDDEN"
        // errorCode, no new "ADMIN_DISABLED" errorCode is invented.
        if (!admin.IsActive)
        {
            throw new ForbiddenException("This admin account is disabled.");
        }

        var token = _jwtTokenGenerator.GenerateAccessToken(admin);

        // Day 13: refresh token is now actually persisted (hashed) and usable at
        // POST /api/v1/auth/refresh - see RefreshToken.cs's XML comment.
        var refresh = _jwtTokenGenerator.GenerateOpaqueRefreshToken();
        await _refreshTokenRepository.AddAsync(
            new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                TokenHash = RefreshTokenHasher.Hash(refresh.RawToken),
                UserType = "Admin",
                UserId = admin.Id,
                ExpiresAt = refresh.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AdminAuthResponse
        {
            AdminId = admin.Id,
            Name = admin.FullName,
            AccessToken = token.AccessToken,
            RefreshToken = refresh.RawToken,
            ExpiresInSeconds = token.ExpiresInSeconds
        };
    }
}
