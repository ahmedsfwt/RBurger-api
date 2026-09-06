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

namespace RBurger.Application.Authentication.Commands.DriverLogin;

public class DriverLoginCommandHandler : IRequestHandler<DriverLoginCommand, DriverAuthResponse>
{
    private readonly IDriverRepository _driverRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public DriverLoginCommandHandler(
        IDriverRepository driverRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _driverRepository = driverRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<DriverAuthResponse> Handle(DriverLoginCommand request, CancellationToken cancellationToken)
    {
        // §7.2: "Authenticate an existing driver by phone + password."
        var driver = await _driverRepository.GetByPhoneAsync(request.Phone, cancellationToken);

        // Approved Day 6 decision #1: credentials are checked first, regardless of IsActive.
        // A disabled driver with a wrong password still gets 401 (INVALID_CREDENTIALS), not 403 -
        // this mirrors CustomerLoginCommandHandler's check order exactly and avoids leaking a
        // disabled-account signal to a caller who doesn't even have the right password.
        if (driver is null || !_passwordHasher.Verify(driver.PasswordHash, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        // §7.6.3: "A disabled driver's login (§7.2) is rejected with 403 even with the correct
        // password." Only reached once credentials are confirmed correct. Approved Day 6
        // decision #2: reuses the existing generic ForbiddenException/"FORBIDDEN" errorCode -
        // no new "DRIVER_DISABLED" errorCode is invented.
        if (!driver.IsActive)
        {
            throw new ForbiddenException("This driver account is disabled.");
        }

        var token = _jwtTokenGenerator.GenerateAccessToken(driver);

        // Day 13: refresh token is now actually persisted (hashed) and usable at
        // POST /api/v1/auth/refresh - see RefreshToken.cs's XML comment.
        var refresh = _jwtTokenGenerator.GenerateOpaqueRefreshToken();
        await _refreshTokenRepository.AddAsync(
            new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                TokenHash = RefreshTokenHasher.Hash(refresh.RawToken),
                UserType = "Driver",
                UserId = driver.Id,
                ExpiresAt = refresh.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new DriverAuthResponse
        {
            DriverId = driver.Id,
            FullName = driver.FullName,
            BranchId = driver.BranchId,
            AccessToken = token.AccessToken,
            RefreshToken = refresh.RawToken,
            ExpiresInSeconds = token.ExpiresInSeconds
        };
    }
}
