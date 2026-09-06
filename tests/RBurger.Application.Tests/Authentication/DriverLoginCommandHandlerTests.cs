using RBurger.Application.Authentication.Commands.DriverLogin;
using RBurger.Application.Common.Exceptions;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Authentication;

public class DriverLoginCommandHandlerTests
{
    private static Driver ActiveDriver(string phone = "01099988877", string password = "P@ssw0rd") => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Karim Adel",
        Phone = phone,
        PasswordHash = $"hashed:{password}",
        Vehicle = "bike",
        BranchId = 1,
        CreatedByAdminId = Guid.NewGuid(),
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static (DriverLoginCommandHandler Handler, FakeDriverRepository Drivers) BuildHandler()
    {
        var drivers = new FakeDriverRepository();
        var handler = new DriverLoginCommandHandler(
            drivers, new FakePasswordHasher(), new FakeJwtTokenGenerator(), new FakeRefreshTokenRepository());
        return (handler, drivers);
    }

    [Fact]
    public async Task Handle_returns_DriverAuthResponse_on_valid_credentials()
    {
        var (handler, drivers) = BuildHandler();
        var driver = ActiveDriver();
        drivers.Drivers.Add(driver);

        var result = await handler.Handle(
            new DriverLoginCommand { Phone = driver.Phone, Password = "P@ssw0rd" }, default);

        Assert.Equal(driver.Id, result.DriverId);
        Assert.Equal(driver.FullName, result.FullName);
        Assert.Equal(driver.BranchId, result.BranchId);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.True(result.ExpiresInSeconds > 0);
    }

    [Fact]
    public async Task Handle_throws_InvalidCredentialsException_when_phone_not_found()
    {
        var (handler, _) = BuildHandler();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new DriverLoginCommand { Phone = "01000000000", Password = "P@ssw0rd" }, default));
    }

    [Fact]
    public async Task Handle_throws_InvalidCredentialsException_when_password_is_wrong()
    {
        var (handler, drivers) = BuildHandler();
        var driver = ActiveDriver();
        drivers.Drivers.Add(driver);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new DriverLoginCommand { Phone = driver.Phone, Password = "WrongPassword" }, default));
    }

    // §7.6.3: "A disabled driver's login is rejected with 403 even with the correct password."
    [Fact]
    public async Task Handle_throws_ForbiddenException_when_driver_is_disabled_and_password_is_correct()
    {
        var (handler, drivers) = BuildHandler();
        var driver = ActiveDriver();
        driver.IsActive = false;
        drivers.Drivers.Add(driver);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DriverLoginCommand { Phone = driver.Phone, Password = "P@ssw0rd" }, default));
    }

    // Approved Day 6 decision #1: credentials are checked first. A disabled driver with a wrong
    // password gets 401 (InvalidCredentialsException), not 403 - it must not leak the
    // disabled-account signal to a caller who doesn't have the right password.
    [Fact]
    public async Task Handle_throws_InvalidCredentialsException_when_driver_is_disabled_and_password_is_wrong()
    {
        var (handler, drivers) = BuildHandler();
        var driver = ActiveDriver();
        driver.IsActive = false;
        drivers.Drivers.Add(driver);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(new DriverLoginCommand { Phone = driver.Phone, Password = "WrongPassword" }, default));
    }
}
