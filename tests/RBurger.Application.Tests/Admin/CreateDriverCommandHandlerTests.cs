using RBurger.Application.Admin.Drivers.Commands.CreateDriver;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class CreateDriverCommandHandlerTests
{
    private static CreateDriverCommand ValidCommand() => new()
    {
        FullName = "Karim Adel",
        Phone = "01099988877",
        Password = "P@ssw0rd",
        Vehicle = "bike",
        BranchId = 1
    };

    private static Branch ExistingBranch() => new()
    {
        Id = 1,
        NameAr = "سوهاج",
        NameEn = "Sohag",
        DeliveryFee = 20,
        EtaMinMinutes = 25,
        EtaMaxMinutes = 35,
        IsActive = true
    };

    private static (CreateDriverCommandHandler Handler, FakeDriverRepository Drivers, FakeBranchRepository Branches,
        FakePasswordHasher Hasher, FakeCurrentUserService CurrentUser) BuildHandler()
    {
        var drivers = new FakeDriverRepository();
        var branches = new FakeBranchRepository();
        var hasher = new FakePasswordHasher();
        var currentUser = new FakeCurrentUserService { AdminId = Guid.NewGuid() };
        var handler = new CreateDriverCommandHandler(drivers, branches, hasher, currentUser);
        return (handler, drivers, branches, hasher, currentUser);
    }

    [Fact]
    public async Task Handle_creates_driver_and_returns_documented_shape()
    {
        var (handler, drivers, branches, _, currentUser) = BuildHandler();
        branches.Branches.Add(ExistingBranch());

        var result = await handler.Handle(ValidCommand(), default);

        Assert.Single(drivers.Drivers);
        Assert.Equal("Karim Adel", result.FullName);
        Assert.Equal("01099988877", result.Phone);
        Assert.Equal("bike", result.Vehicle);
        Assert.Equal(1, result.BranchId);
        Assert.True(result.IsActive); // §6.2: default 1
        Assert.Equal(currentUser.AdminId, drivers.Drivers[0].CreatedByAdminId); // §6.3
    }

    [Fact]
    public async Task Handle_hashes_the_password_instead_of_storing_it_raw()
    {
        var (handler, drivers, branches, hasher, _) = BuildHandler();
        branches.Branches.Add(ExistingBranch());

        await handler.Handle(ValidCommand(), default);

        Assert.Equal(hasher.Hash("P@ssw0rd"), drivers.Drivers[0].PasswordHash);
        Assert.NotEqual("P@ssw0rd", drivers.Drivers[0].PasswordHash);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_branch_does_not_exist()
    {
        var (handler, _, _, _, _) = BuildHandler();
        var command = ValidCommand();
        command.BranchId = 999;

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, default));
    }

    [Fact]
    public async Task Handle_throws_ConflictException_when_phone_already_taken()
    {
        var (handler, drivers, branches, _, _) = BuildHandler();
        branches.Branches.Add(ExistingBranch());
        drivers.Drivers.Add(new Driver { Id = Guid.NewGuid(), Phone = "01099988877", BranchId = 1 });

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(ValidCommand(), default));
        Assert.Equal("DUPLICATE_PHONE", ex.ErrorCode);
    }
}
