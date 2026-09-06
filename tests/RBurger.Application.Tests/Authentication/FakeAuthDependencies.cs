using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Models;
using RBurger.Domain.Entities;
// Day 10 fix (same root cause as IAdminRepository.cs/IJwtTokenGenerator.cs in the Application
// project): tests/RBurger.Application.Tests/Admin/AdminFakes.cs introduced a nested
// "RBurger.Application.Tests.Admin" namespace, which shadows bare "Admin" for every file under
// RBurger.Application.Tests before using directives are consulted (CS0118). Aliased instead of
// renaming the Domain entity or the feature namespace.
using AdminEntity = RBurger.Domain.Entities.Admin;

namespace RBurger.Application.Tests.Authentication;

// Day 6 addition - simple in-memory fakes, following the exact same dependency-free pattern as
// tests/RBurger.Application.Tests/Orders/FakeRepositories.cs (no mocking library referenced by
// this test project).
internal class FakeDriverRepository : IDriverRepository
{
    public List<Driver> Drivers { get; } = new();

    public Task<Driver?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return Task.FromResult(Drivers.FirstOrDefault(d => d.Phone == phone));
    }

    // Day 7 addition - see IDriverRepository's XML comment.
    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Drivers.FirstOrDefault(d => d.Id == id));
    }

    // Day 10 addition - see IDriverRepository's XML comment.
    public Task<bool> HasDriversForBranchAsync(int branchId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Drivers.Any(d => d.BranchId == branchId));
    }

    // ---- Day 11 additions (§7.6.3 Admin Driver Management) ----

    public Task AddAsync(Driver driver, CancellationToken cancellationToken)
    {
        Drivers.Add(driver);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<Driver> Drivers, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var ordered = Drivers.OrderBy(d => d.CreatedAt).ToList();
        var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IReadOnlyList<Driver>)paged, ordered.Count));
    }

    public Task DeleteAsync(Driver driver, CancellationToken cancellationToken)
    {
        Drivers.RemoveAll(d => d.Id == driver.Id);
        return Task.CompletedTask;
    }

    // In-memory fake has no FK-Restrict concept to simulate (that's DriverRepository's
    // real-EF-only defensive DbUpdateException fallback for terminal order history - see its
    // XML comment); mirrors FakeBranchRepository.SaveChangesAsync's identical no-op rationale.
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    // ---- Day 12 addition (§7.6.6 Analytics) ----

    public Task<List<Driver>> GetAllActiveAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Drivers.Where(d => d.IsActive).ToList());
    }
}

// Day 11 addition - simple in-memory fake for ICustomerRepository, mirroring
// FakeDriverRepository above (no equivalent Customer fake existed prior to Day 11 - the
// pre-Day-11 CustomerSignup tests only exercised the validator, not a handler with fakes).
internal class FakeCustomerRepository : ICustomerRepository
{
    public List<Customer> Customers { get; } = new();

    public Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return Task.FromResult(Customers.FirstOrDefault(c => c.Phone == phone));
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Customers.FirstOrDefault(c => c.Id == id));
    }

    public Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return Task.FromResult(Customers.Any(c => c.Phone == phone));
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        Customers.Add(customer);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<Customer> Customers, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var ordered = Customers.OrderBy(c => c.CreatedAt).ToList();
        var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IReadOnlyList<Customer>)paged, ordered.Count));
    }

    // Unlike the real EF implementation (which relies on CustomerConfiguration's
    // OnDelete(DeleteBehavior.SetNull) to atomically null out referencing Order.CustomerId
    // columns as part of the same DB transaction/DELETE statement - a DB engine feature, not
    // Application-layer logic), this in-memory fake has no DB-level cascade to simulate. Only
    // removal from this in-memory list is observable here; the cascade-onto-Orders behavior is
    // not exercisable without a real database - see the Day 11 report's Deferred Items,
    // mirroring the exact same honest limitation already documented for
    // FakeBranchRepository.SaveChangesAsync's FK-Restrict case in Day 10.
    public Task DeleteAsync(Customer customer, CancellationToken cancellationToken)
    {
        Customers.RemoveAll(c => c.Id == customer.Id);
        return Task.CompletedTask;
    }
}

// Day 10 addition - simple in-memory fake for IAdminRepository, mirroring FakeDriverRepository.
internal class FakeAdminRepository : IAdminRepository
{
    public List<AdminEntity> Admins { get; } = new();

    public Task<AdminEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return Task.FromResult(Admins.FirstOrDefault(a => a.Username == username));
    }

    // Day 13 addition - see IAdminRepository's XML comment.
    public Task<AdminEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Admins.FirstOrDefault(a => a.Id == id));
    }
}

// Fakes password verification with a plain equality check against a "hashed:" prefix, so tests
// don't need the real Identity PasswordHasher - only the Verify/Hash contract matters here.
internal class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string hashedPassword, string providedPassword) =>
        hashedPassword == $"hashed:{providedPassword}";
}

internal class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    // Day 13 addition - lets tests control the refresh token's TTL deterministically without
    // depending on RefreshTokenSettings/DI (mirrors FakePaymentProvider's AlwaysThrow-style
    // toggle pattern). Defaults to the approved 30-day TTL.
    public int RefreshTokenExpiresInDays { get; set; } = 30;

    // Deterministic (not random) so tests can assert two calls never collide by coincidence
    // and so RefreshTokenCommandHandlerTests can construct a request using a known raw value.
    private int _refreshTokenCounter;

    public JwtTokenResult GenerateAccessToken(Customer customer) =>
        new($"fake-customer-token-{customer.Id}", 3600);

    public JwtTokenResult GenerateAccessToken(Driver driver) =>
        new($"fake-driver-token-{driver.Id}", 3600);

    // Day 10 addition - mirrors the Customer/Driver overloads.
    public JwtTokenResult GenerateAccessToken(AdminEntity admin) =>
        new($"fake-admin-token-{admin.Id}", 3600);

    // Day 13: real persisted-and-validated tokens (see IJwtTokenGenerator's XML comment).
    public RefreshTokenResult GenerateOpaqueRefreshToken() =>
        new($"fake-refresh-token-{Guid.NewGuid()}-{_refreshTokenCounter++}",
            DateTime.UtcNow.AddDays(RefreshTokenExpiresInDays));
}

// Day 13 addition - simple in-memory fake for IRefreshTokenRepository, following the exact
// same dependency-free pattern as every other fake in this file. TryRevokeAsync/
// RevokeAllActiveForUserAsync reproduce the real EF implementation's conditional-UPDATE
// concurrency guard (only succeeds if RevokedAt is still null at the moment of the call) in
// plain C#, so RefreshTokenCommandHandlerTests can assert the same reuse/race behavior without
// a real database.
internal class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RBurger.Domain.Entities.RefreshToken> Tokens { get; } = new();

    public Task AddAsync(RBurger.Domain.Entities.RefreshToken token, CancellationToken cancellationToken)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RBurger.Domain.Entities.RefreshToken?> GetByTokenHashAsync(
        string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));
    }

    public Task<bool> TryRevokeAsync(Guid tokenId, Guid replacedByTokenId, CancellationToken cancellationToken)
    {
        var token = Tokens.FirstOrDefault(t => t.Id == tokenId);
        if (token is null || token.RevokedAt is not null)
        {
            return Task.FromResult(false);
        }

        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByTokenId = replacedByTokenId;
        return Task.FromResult(true);
    }

    public Task RevokeAllActiveForUserAsync(string userType, Guid userId, CancellationToken cancellationToken)
    {
        foreach (var token in Tokens.Where(t =>
                     t.UserType == userType && t.UserId == userId && t.RevokedAt is null))
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    // Day 14 addition (Backend Parity Spec §5 - logout).
    public Task RevokeAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        var token = Tokens.FirstOrDefault(t => t.Id == tokenId);
        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// Day 12 addition - fakes the scaffold-only IPaymentProvider contract, following the exact
// same "AlwaysThrow toggle" pattern already established by AdminFakes.cs's
// FakeMenuItemImageStorage for the equivalent IMenuItemImageStorage scaffold.
// AlwaysThrow = true (the default) mirrors NotConfiguredPaymentProvider's real Infrastructure
// behavior; AlwaysThrow = false lets tests assert the fully-specified success-path persistence
// (§9.4's card-refund branch, §9.3's charge/webhook flow) without depending on a real gateway.
internal class FakePaymentProvider : IPaymentProvider
{
    public bool AlwaysThrow { get; set; } = true;

    // Day 13 addition - lets DeleteAdminOrderCommandHandlerTests exercise §9.4's "the external
    // refund fails" branch (a real gateway reporting Success=false) distinctly from the
    // AlwaysThrow scaffold-not-configured case (which throws instead of returning a result).
    public bool NextRefundSucceeds { get; set; } = true;

    public List<(Guid PaymentId, decimal Amount)> RefundCalls { get; } = new();
    public List<(Guid OrderId, decimal Amount, string Currency)> ChargeCalls { get; } = new();

    public Task<PaymentSession> CreateSessionAsync(Guid orderId, decimal amount, string currency)
    {
        if (AlwaysThrow)
        {
            throw new PaymentProviderNotConfiguredException();
        }

        ChargeCalls.Add((orderId, amount, currency));
        return Task.FromResult(new PaymentSession("fake-session", "https://fake-gateway.test/pay"));
    }

    public Task<RefundResult> RefundAsync(Guid paymentId, decimal amount)
    {
        if (AlwaysThrow)
        {
            throw new PaymentProviderNotConfiguredException();
        }

        RefundCalls.Add((paymentId, amount));
        return Task.FromResult(new RefundResult(NextRefundSucceeds, NextRefundSucceeds ? "fake-refund-ref" : null));
    }

    public bool ValidateWebhookSignature(string payload, string signatureHeader)
    {
        if (AlwaysThrow)
        {
            throw new PaymentProviderNotConfiguredException();
        }

        return true;
    }
}
