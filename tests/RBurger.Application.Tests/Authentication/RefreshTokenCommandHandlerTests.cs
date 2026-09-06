using RBurger.Application.Authentication.Commands.RefreshToken;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Security;
using RBurger.Domain.Entities;
using Xunit;
// Day 13 fix - same class of bug as the Admin/RefreshToken namespace collisions fixed
// elsewhere (see IAdminRepository.cs / the four login handlers' comments): this test file's
// own namespace segment doesn't collide, but the entity type is aliased anyway for clarity
// and consistency with every other file that constructs a RefreshToken entity directly.
using RefreshTokenEntity = RBurger.Domain.Entities.RefreshToken;

namespace RBurger.Application.Tests.Authentication;

public class RefreshTokenCommandHandlerTests
{
    private static Customer ActiveCustomer() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Ahmed Sami",
        Phone = "01012345678",
        PasswordHash = "hashed:P@ssw0rd",
        PreferredLanguage = "ar",
        CreatedAt = DateTime.UtcNow
    };

    private static Driver ActiveDriver(bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Karim Adel",
        Phone = "01099988877",
        PasswordHash = "hashed:P@ssw0rd",
        Vehicle = "bike",
        BranchId = 1,
        CreatedByAdminId = Guid.NewGuid(),
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    private static (
        RefreshTokenCommandHandler Handler,
        FakeRefreshTokenRepository Tokens,
        FakeCustomerRepository Customers,
        FakeDriverRepository Drivers,
        FakeAdminRepository Admins,
        FakeJwtTokenGenerator Jwt)
        BuildHandler()
    {
        var tokens = new FakeRefreshTokenRepository();
        var customers = new FakeCustomerRepository();
        var drivers = new FakeDriverRepository();
        var admins = new FakeAdminRepository();
        var jwt = new FakeJwtTokenGenerator();
        var handler = new RefreshTokenCommandHandler(tokens, customers, drivers, admins, jwt);
        return (handler, tokens, customers, drivers, admins, jwt);
    }

    // Seeds a live (non-revoked, non-expired) RefreshToken row for the given identity and
    // returns the raw value the "client" would present - exactly what
    // IJwtTokenGenerator.GenerateOpaqueRefreshToken()/RefreshTokenHasher.Hash produce together
    // in the real login/signup flow.
    private static string SeedToken(
        FakeRefreshTokenRepository tokens,
        string userType,
        Guid userId,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null)
    {
        var raw = $"raw-{Guid.NewGuid()}";
        tokens.Tokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            TokenHash = RefreshTokenHasher.Hash(raw),
            UserType = userType,
            UserId = userId,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = revokedAt
        });
        return raw;
    }

    [Fact]
    public async Task Handle_valid_customer_token_returns_new_access_token()
    {
        var (handler, tokens, customers, _, _, _) = BuildHandler();
        var customer = ActiveCustomer();
        customers.Customers.Add(customer);
        var raw = SeedToken(tokens, "Customer", customer.Id);

        var result = await handler.Handle(new RefreshTokenCommand(raw), default);

        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.Equal(3600, result.ExpiresInSeconds);
    }

    // Day 16 (Backend Parity Spec §3 - approved contract change): the response now includes
    // the replacement refreshToken alongside accessToken/expiresInSeconds.
    [Fact]
    public async Task Handle_response_contains_the_replacement_refresh_token()
    {
        var (handler, tokens, customers, _, _, _) = BuildHandler();
        var customer = ActiveCustomer();
        customers.Customers.Add(customer);
        var raw = SeedToken(tokens, "Customer", customer.Id);
        var originalTokenId = tokens.Tokens.Single().Id;

        var result = await handler.Handle(new RefreshTokenCommand(raw), default);

        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.NotEqual(raw, result.RefreshToken); // a genuinely new token, not the old one echoed back

        var replacement = tokens.Tokens.Single(t => t.Id != originalTokenId);
        Assert.Equal(RefreshTokenHasher.Hash(result.RefreshToken), replacement.TokenHash);
    }

    // The replacement token returned in the response must itself be immediately usable for a
    // subsequent refresh - proving it was actually persisted, not just generated in-memory.
    [Fact]
    public async Task Handle_returned_replacement_refresh_token_can_itself_be_used_to_refresh_again()
    {
        var (handler, tokens, customers, _, _, _) = BuildHandler();
        var customer = ActiveCustomer();
        customers.Customers.Add(customer);
        var raw = SeedToken(tokens, "Customer", customer.Id);

        var first = await handler.Handle(new RefreshTokenCommand(raw), default);
        var second = await handler.Handle(new RefreshTokenCommand(first.RefreshToken), default);

        Assert.False(string.IsNullOrEmpty(second.AccessToken));
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
    }

    [Fact]
    public async Task Handle_rotation_revokes_old_token_and_persists_a_replacement()
    {
        var (handler, tokens, customers, _, _, _) = BuildHandler();
        var customer = ActiveCustomer();
        customers.Customers.Add(customer);
        var raw = SeedToken(tokens, "Customer", customer.Id);
        var originalTokenId = tokens.Tokens.Single().Id;

        await handler.Handle(new RefreshTokenCommand(raw), default);

        var original = tokens.Tokens.Single(t => t.Id == originalTokenId);
        Assert.NotNull(original.RevokedAt);

        var replacement = tokens.Tokens.Single(t => t.Id != originalTokenId);
        Assert.Equal(original.ReplacedByTokenId, replacement.Id);
        Assert.Null(replacement.RevokedAt);
        Assert.Equal("Customer", replacement.UserType);
        Assert.Equal(customer.Id, replacement.UserId);

        // Approved Day 13 decision: 30-day default TTL (RefreshTokenSettings.ExpiresInDays).
        Assert.True(
            (replacement.ExpiresAt - DateTime.UtcNow).TotalDays is > 29.9 and < 30.1,
            "Replacement token should use the approved 30-day TTL.");
    }

    [Fact]
    public async Task Handle_throws_InvalidCredentialsException_for_unknown_token()
    {
        var (handler, _, _, _, _, _) = BuildHandler();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new RefreshTokenCommand("not-a-real-token"), default));
    }

    [Fact]
    public async Task Handle_throws_InvalidCredentialsException_for_expired_token()
    {
        var (handler, tokens, customers, _, _, _) = BuildHandler();
        var customer = ActiveCustomer();
        customers.Customers.Add(customer);
        var raw = SeedToken(tokens, "Customer", customer.Id, expiresAt: DateTime.UtcNow.AddSeconds(-1));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new RefreshTokenCommand(raw), default));
    }

    [Fact]
    public async Task Handle_throws_InvalidCredentialsException_for_already_revoked_token()
    {
        var (handler, tokens, customers, _, _, _) = BuildHandler();
        var customer = ActiveCustomer();
        customers.Customers.Add(customer);
        var raw = SeedToken(tokens, "Customer", customer.Id, revokedAt: DateTime.UtcNow.AddMinutes(-5));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new RefreshTokenCommand(raw), default));
    }

    // Reuse-detection defense: presenting an already-rotated token again must invalidate every
    // other still-active token for that identity too, not just this one call.
    [Fact]
    public async Task Handle_reusing_a_rotated_token_revokes_all_other_active_tokens_for_the_user()
    {
        var (handler, tokens, customers, _, _, _) = BuildHandler();
        var customer = ActiveCustomer();
        customers.Customers.Add(customer);
        var raw = SeedToken(tokens, "Customer", customer.Id);
        var otherActiveRaw = SeedToken(tokens, "Customer", customer.Id); // e.g. a second device

        // First use: legitimate rotation.
        await handler.Handle(new RefreshTokenCommand(raw), default);

        // Second use of the SAME now-revoked raw token: reuse/theft-detection response.
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new RefreshTokenCommand(raw), default));

        var otherToken = tokens.Tokens.Single(t => t.TokenHash == RefreshTokenHasher.Hash(otherActiveRaw));
        Assert.NotNull(otherToken.RevokedAt); // forced revoked by the reuse-detection response
    }

    // Concurrent/reuse scenario: two callers racing to present the exact same raw token. The
    // conditional TryRevokeAsync guard (mirrored by FakeRefreshTokenRepository) ensures only
    // one of the two ever succeeds - simulated here as two sequential Handle calls with the
    // same raw token, which is exactly the observable outcome of a real race (one winner, one
    // loser), without needing a multi-threaded test harness against an in-memory fake.
    [Fact]
    public async Task Handle_concurrent_use_of_the_same_token_only_lets_one_caller_succeed()
    {
        var (handler, tokens, customers, _, _, _) = BuildHandler();
        var customer = ActiveCustomer();
        customers.Customers.Add(customer);
        var raw = SeedToken(tokens, "Customer", customer.Id);

        var first = await handler.Handle(new RefreshTokenCommand(raw), default);
        Assert.False(string.IsNullOrEmpty(first.AccessToken));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new RefreshTokenCommand(raw), default));

        // Exactly one successor token was minted, not two.
        Assert.Equal(2, tokens.Tokens.Count); // original (revoked) + single replacement
    }

    // Customer isolation: refreshing Customer A's token must never produce a token for
    // Customer B, and must never affect Customer B's own refresh token.
    [Fact]
    public async Task Handle_customer_isolation_refreshing_one_customer_never_touches_another()
    {
        var (handler, tokens, customers, _, _, _) = BuildHandler();
        var customerA = ActiveCustomer();
        var customerB = ActiveCustomer();
        customers.Customers.Add(customerA);
        customers.Customers.Add(customerB);
        var rawA = SeedToken(tokens, "Customer", customerA.Id);
        var rawB = SeedToken(tokens, "Customer", customerB.Id);

        await handler.Handle(new RefreshTokenCommand(rawA), default);

        var tokenBRow = tokens.Tokens.Single(t => t.TokenHash == RefreshTokenHasher.Hash(rawB));
        Assert.Null(tokenBRow.RevokedAt); // untouched by Customer A's refresh

        // Customer B's own token still works independently.
        var resultB = await handler.Handle(new RefreshTokenCommand(rawB), default);
        Assert.False(string.IsNullOrEmpty(resultB.AccessToken));
    }

    // §7.6.3-style re-verification extended to the refresh flow (Day 13 decision, flagged in
    // the report): a driver disabled after the refresh token was issued must not keep minting
    // fresh access tokens.
    [Fact]
    public async Task Handle_throws_ForbiddenException_when_driver_was_disabled_after_token_issuance()
    {
        var (handler, tokens, _, drivers, _, _) = BuildHandler();
        var driver = ActiveDriver(isActive: false);
        drivers.Drivers.Add(driver);
        var raw = SeedToken(tokens, "Driver", driver.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new RefreshTokenCommand(raw), default));
    }
}
