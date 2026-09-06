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

namespace RBurger.Application.Authentication.Commands.CustomerSignup;

public class CustomerSignupCommandHandler : IRequestHandler<CustomerSignupCommand, AuthResponse>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public CustomerSignupCommandHandler(
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

    public async Task<AuthResponse> Handle(CustomerSignupCommand request, CancellationToken cancellationToken)
    {
        // §6.2: Customers.Phone is unique. §7.8: 409 Conflict on "duplicate phone on customer signup".
        var phoneTaken = await _customerRepository.ExistsByPhoneAsync(request.Phone, cancellationToken);
        if (phoneTaken)
        {
            throw new ConflictException("A customer with this phone number already exists.", "DUPLICATE_PHONE");
        }

        var customer = new Customer
        {
            // Approved Day 2 decision #6: client-generated GUID (ValueGeneratedNever).
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone,
            PasswordHash = _passwordHasher.Hash(request.Password),
            DefaultAddress = request.Address,
            PreferredLanguage = request.PreferredLanguage,
            // CreatedAt intentionally left unset: §6.2's documented DB default
            // (GETUTCDATE(), configured in Day 2's CustomerConfiguration) populates it on insert.
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _customerRepository.SaveChangesAsync(cancellationToken);

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
