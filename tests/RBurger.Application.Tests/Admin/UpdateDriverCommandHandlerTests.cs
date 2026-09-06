using RBurger.Application.Admin.Drivers.Commands.UpdateDriver;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class UpdateDriverCommandHandlerTests
{
    private static Driver ExistingDriver() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Karim Adel",
        Phone = "01099988877",
        PasswordHash = "hashed:P@ssw0rd",
        Vehicle = "bike",
        BranchId = 1,
        IsActive = true
    };

    private static Branch OtherBranch() => new()
    {
        Id = 2,
        NameAr = "جرجا",
        NameEn = "Girga",
        DeliveryFee = 25,
        EtaMinMinutes = 30,
        EtaMaxMinutes = 45,
        IsActive = true
    };

    private static (UpdateDriverCommandHandler Handler, FakeDriverRepository Drivers, FakeBranchRepository Branches,
        FakePasswordHasher Hasher) BuildHandler()
    {
        var drivers = new FakeDriverRepository();
        var branches = new FakeBranchRepository();
        var hasher = new FakePasswordHasher();
        var handler = new UpdateDriverCommandHandler(drivers, branches, hasher);
        return (handler, drivers, branches, hasher);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_driver_does_not_exist()
    {
        var (handler, _, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateDriverCommand { Id = Guid.NewGuid() }, default));
    }

    [Fact]
    public async Task Handle_updates_vehicle_and_branch()
    {
        var (handler, drivers, branches, _) = BuildHandler();
        var driver = ExistingDriver();
        drivers.Drivers.Add(driver);
        branches.Branches.Add(OtherBranch());

        var result = await handler.Handle(
            new UpdateDriverCommand { Id = driver.Id, Vehicle = "car", BranchId = 2 }, default);

        Assert.Equal("car", result.Vehicle);
        Assert.Equal(2, result.BranchId);
        Assert.Equal("car", driver.Vehicle);
        Assert.Equal(2, driver.BranchId);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_new_branch_does_not_exist()
    {
        var (handler, drivers, _, _) = BuildHandler();
        var driver = ExistingDriver();
        drivers.Drivers.Add(driver);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateDriverCommand { Id = driver.Id, BranchId = 999 }, default));
    }

    [Fact]
    public async Task Handle_updates_full_name_when_provided()
    {
        var (handler, drivers, _, _) = BuildHandler();
        var driver = ExistingDriver();
        drivers.Drivers.Add(driver);

        await handler.Handle(new UpdateDriverCommand { Id = driver.Id, FullName = "Karim Sami" }, default);

        Assert.Equal("Karim Sami", driver.FullName);
    }

    [Fact]
    public async Task Handle_resets_password_hash_when_password_provided()
    {
        var (handler, drivers, _, hasher) = BuildHandler();
        var driver = ExistingDriver();
        drivers.Drivers.Add(driver);
        var originalHash = driver.PasswordHash;

        await handler.Handle(new UpdateDriverCommand { Id = driver.Id, Password = "NewP@ss1" }, default);

        Assert.NotEqual(originalHash, driver.PasswordHash);
        Assert.Equal(hasher.Hash("NewP@ss1"), driver.PasswordHash);
    }

    [Fact]
    public async Task Handle_leaves_unspecified_fields_unchanged()
    {
        var (handler, drivers, _, _) = BuildHandler();
        var driver = ExistingDriver();
        drivers.Drivers.Add(driver);

        await handler.Handle(new UpdateDriverCommand { Id = driver.Id }, default);

        Assert.Equal("Karim Adel", driver.FullName);
        Assert.Equal("bike", driver.Vehicle);
        Assert.Equal(1, driver.BranchId);
        Assert.Equal("hashed:P@ssw0rd", driver.PasswordHash);
    }
}
