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

namespace RBurger.Application.Authentication.Commands.CustomerLogin;

public class CustomerLoginCommandHandler : IRequestHandler<CustomerLoginCommand, AuthResponse>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public CustomerLoginCommandHandler(
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponse> Handle(CustomerLoginCommand request, CancellationToken cancellationToken)
    {
        // §7.1: "Authenticate an existing customer by phone + password."
        var customer = await _customerRepository.GetByPhoneAsync(request.Phone, cancellationToken);
        if (customer is null || !_passwordHasher.Verify(customer.PasswordHash, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        var token = _jwtTokenGenerator.GenerateAccessToken(customer);

        // Day 13: refresh token is now actually persisted (hashed) and usable at
        // POST /api/v1/auth/refresh - see RefreshToken.cs's XML comment.
        var refresh = _jwtTokenGenerator.GenerateOpaqueRefreshToken();
        await _refreshTokenRepository.AddAsync(
            new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                TokenHash = RefreshTokenHasher.Hash(refresh.RawToken),
                UserType = "Customer",
                UserId = customer.Id,
                ExpiresAt = refresh.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            CustomerId = customer.Id,
            FullName = customer.FullName,
            AccessToken = token.AccessToken,
            RefreshToken = refresh.RawToken,
            ExpiresInSeconds = token.ExpiresInSeconds
        };
    }
}
