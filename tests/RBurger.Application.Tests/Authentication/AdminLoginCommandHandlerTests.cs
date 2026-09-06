using RBurger.Application.Authentication.Commands.AdminLogin;
using RBurger.Application.Common.Exceptions;
using RBurger.Domain.Entities;
// Day 10 fix - see FakeAuthDependencies.cs's comment for the full explanation.
using AdminEntity = RBurger.Domain.Entities.Admin;
using Xunit;

namespace RBurger.Application.Tests.Authentication;

public class AdminLoginCommandHandlerTests
{
    private static AdminEntity ActiveAdmin(string username = "admin", string password = "P@ssw0rd") => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        FullName = "Admin",
        PasswordHash = $"hashed:{password}",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static (AdminLoginCommandHandler Handler, FakeAdminRepository Admins) BuildHandler()
    {
        var admins = new FakeAdminRepository();
        var handler = new AdminLoginCommandHandler(
            admins, new FakePasswordHasher(), new FakeJwtTokenGenerator(), new FakeRefreshTokenRepository());
        return (handler, admins);
    }

    [Fact]
    public async Task Handle_returns_AdminAuthResponse_on_valid_credentials()
    {
        var (handler, admins) = BuildHandler();
        var admin = ActiveAdmin();
        admins.Admins.Add(admin);

        var result = await handler.Handle(
            new AdminLoginCommand { Username = admin.Username, Password = "P@ssw0rd" }, default);

        Assert.Equal(admin.Id, result.AdminId);
        Assert.Equal(admin.FullName, result.Name);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.True(result.ExpiresInSeconds > 0);
    }

    [Fact]
    public async Task Handle_throws_InvalidCredentialsException_when_username_not_found()
    {
        var (handler, _) = BuildHandler();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new AdminLoginCommand { Username = "nobody", Password = "P@ssw0rd" }, default));
    }

    [Fact]
    public async Task Handle_throws_InvalidCredentialsException_when_password_is_wrong()
    {
        var (handler, admins) = BuildHandler();
        var admin = ActiveAdmin();
        admins.Admins.Add(admin);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new AdminLoginCommand { Username = admin.Username, Password = "WrongPassword" }, default));
    }

    // Day 10 task brief: "Disabled Admin behavior must follow the existing authentication
    // conventions" - mirrors §7.6.3's disabled-driver-403 rule.
    [Fact]
    public async Task Handle_throws_ForbiddenException_when_admin_is_disabled_and_password_is_correct()
    {
        var (handler, admins) = BuildHandler();
        var admin = ActiveAdmin();
        admin.IsActive = false;
        admins.Admins.Add(admin);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new AdminLoginCommand { Username = admin.Username, Password = "P@ssw0rd" }, default));
    }

    // Mirrors the approved Day 6 decision applied to Admin: credentials checked first, so a
    // disabled admin with a wrong password gets 401, never leaking the disabled-account signal.
    [Fact]
    public async Task Handle_throws_InvalidCredentialsException_when_admin_is_disabled_and_password_is_wrong()
    {
        var (handler, admins) = BuildHandler();
        var admin = ActiveAdmin();
        admin.IsActive = false;
        admins.Admins.Add(admin);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new AdminLoginCommand { Username = admin.Username, Password = "WrongPassword" }, default));
    }
}
