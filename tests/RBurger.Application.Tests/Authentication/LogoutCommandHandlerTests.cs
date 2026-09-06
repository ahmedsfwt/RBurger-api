using RBurger.Application.Authentication.Commands.Logout;
using RBurger.Application.Authentication.Commands.RefreshToken;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Security;
using RBurger.Domain.Entities;
using Xunit;
using RefreshTokenEntity = RBurger.Domain.Entities.RefreshToken;

namespace RBurger.Application.Tests.Authentication;

// Day 14 addition (Backend Parity Spec §5).
public class LogoutCommandHandlerTests
{
    private static string SeedToken(FakeRefreshTokenRepository tokens, Guid userId, DateTime? revokedAt = null)
    {
        var raw = $"raw-{Guid.NewGuid()}";
        tokens.Tokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            TokenHash = RefreshTokenHasher.Hash(raw),
            UserType = "Customer",
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = revokedAt
        });
        return raw;
    }

    [Fact]
    public async Task Handle_valid_logout_revokes_the_token()
    {
        var tokens = new FakeRefreshTokenRepository();
        var raw = SeedToken(tokens, Guid.NewGuid());
        var handler = new LogoutCommandHandler(tokens);

        await handler.Handle(new LogoutCommand(raw), default);

        Assert.NotNull(tokens.Tokens.Single().RevokedAt);
    }

    [Fact]
    public async Task Handle_revoked_token_cannot_subsequently_refresh()
    {
        var tokens = new FakeRefreshTokenRepository();
        var customers = new FakeCustomerRepository();
        var customer = new Customer
        {
            Id = Guid.NewGuid(), FullName = "Ahmed Sami", Phone = "01012345678",
            PasswordHash = "x", CreatedAt = DateTime.UtcNow
        };
        customers.Customers.Add(customer);
        var raw = SeedToken(tokens, customer.Id);

        await new LogoutCommandHandler(tokens).Handle(new LogoutCommand(raw), default);

        var refreshHandler = new RefreshTokenCommandHandler(
            tokens, customers, new FakeDriverRepository(), new FakeAdminRepository(), new FakeJwtTokenGenerator());

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            refreshHandler.Handle(new RefreshTokenCommand(raw), default));
    }

    [Fact]
    public async Task Handle_already_revoked_token_is_idempotent()
    {
        var tokens = new FakeRefreshTokenRepository();
        var revokedAt = DateTime.UtcNow.AddMinutes(-5);
        var raw = SeedToken(tokens, Guid.NewGuid(), revokedAt: revokedAt);
        var handler = new LogoutCommandHandler(tokens);

        await handler.Handle(new LogoutCommand(raw), default); // does not throw

        // RevokedAt is untouched - not bumped forward on a repeat call.
        Assert.Equal(revokedAt, tokens.Tokens.Single().RevokedAt);
    }

    [Fact]
    public async Task Handle_unknown_token_does_not_throw()
    {
        var handler = new LogoutCommandHandler(new FakeRefreshTokenRepository());

        var exception = await Record.ExceptionAsync(() =>
            handler.Handle(new LogoutCommand("not-a-real-token"), default));

        Assert.Null(exception);
    }
}
