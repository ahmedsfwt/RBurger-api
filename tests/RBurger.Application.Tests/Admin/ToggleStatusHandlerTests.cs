using RBurger.Application.Admin.Branches.Commands.ToggleBranchStatus;
using RBurger.Application.Admin.Drivers.Commands.ToggleDriverStatus;
using RBurger.Application.Admin.Menu.Commands.ToggleMenuItemAvailability;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

// Day 14 addition (Backend Parity Spec §2) - covers all three toggle endpoints.
public class ToggleStatusHandlerTests
{
    [Fact]
    public async Task ToggleBranchStatus_flips_current_value()
    {
        var branches = new FakeBranchRepository();
        var branch = new Branch { Id = 1, NameAr = "سوهاج", NameEn = "Sohag", IsActive = true };
        branches.Branches.Add(branch);
        var handler = new ToggleBranchStatusCommandHandler(branches);

        var first = await handler.Handle(new ToggleBranchStatusCommand(1), default);
        Assert.False(first.IsActive);

        var second = await handler.Handle(new ToggleBranchStatusCommand(1), default);
        Assert.True(second.IsActive); // toggling twice returns to the original state
    }

    [Fact]
    public async Task ToggleBranchStatus_throws_NotFoundException_for_unknown_id()
    {
        var handler = new ToggleBranchStatusCommandHandler(new FakeBranchRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ToggleBranchStatusCommand(999), default));
    }

    [Fact]
    public async Task ToggleMenuItemAvailability_flips_current_value()
    {
        var menuItems = new FakeMenuItemRepository();
        var item = new MenuItem { Id = 101, NameAr = "أ", NameEn = "Original", CategoryId = 1, BranchId = 1, IsAvailable = true };
        menuItems.MenuItems.Add(item);
        var handler = new ToggleMenuItemAvailabilityCommandHandler(menuItems);

        var result = await handler.Handle(new ToggleMenuItemAvailabilityCommand(101), default);

        Assert.False(result.IsAvailable);
        Assert.False(item.IsAvailable);
    }

    [Fact]
    public async Task ToggleMenuItemAvailability_throws_NotFoundException_for_unknown_id()
    {
        var handler = new ToggleMenuItemAvailabilityCommandHandler(new FakeMenuItemRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ToggleMenuItemAvailabilityCommand(999), default));
    }

    [Fact]
    public async Task ToggleDriverStatus_flips_current_value()
    {
        var drivers = new FakeDriverRepository();
        var driver = new Driver
        {
            Id = Guid.NewGuid(), FullName = "Karim Adel", Phone = "1", PasswordHash = "x",
            Vehicle = "bike", BranchId = 1, IsActive = true
        };
        drivers.Drivers.Add(driver);
        var handler = new ToggleDriverStatusCommandHandler(drivers);

        var result = await handler.Handle(new ToggleDriverStatusCommand(driver.Id), default);

        Assert.False(result.IsActive);
        Assert.False(driver.IsActive);
    }

    [Fact]
    public async Task ToggleDriverStatus_throws_NotFoundException_for_unknown_id()
    {
        var handler = new ToggleDriverStatusCommandHandler(new FakeDriverRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ToggleDriverStatusCommand(Guid.NewGuid()), default));
    }
}
