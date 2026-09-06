using RBurger.Application.Admin.Drivers.Commands.UpdateDriverStatus;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Authentication;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class UpdateDriverStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_throws_NotFoundException_when_driver_does_not_exist()
    {
        var drivers = new FakeDriverRepository();
        var handler = new UpdateDriverStatusCommandHandler(drivers);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateDriverStatusCommand { Id = Guid.NewGuid(), IsActive = false }, default));
    }

    [Fact]
    public async Task Handle_deactivates_an_active_driver()
    {
        var drivers = new FakeDriverRepository();
        var driver = new Driver { Id = Guid.NewGuid(), IsActive = true };
        drivers.Drivers.Add(driver);
        var handler = new UpdateDriverStatusCommandHandler(drivers);

        var result = await handler.Handle(new UpdateDriverStatusCommand { Id = driver.Id, IsActive = false }, default);

        Assert.False(result.IsActive);
        Assert.False(driver.IsActive);
    }

    [Fact]
    public async Task Handle_reactivates_a_disabled_driver()
    {
        var drivers = new FakeDriverRepository();
        var driver = new Driver { Id = Guid.NewGuid(), IsActive = false };
        drivers.Drivers.Add(driver);
        var handler = new UpdateDriverStatusCommandHandler(drivers);

        var result = await handler.Handle(new UpdateDriverStatusCommand { Id = driver.Id, IsActive = true }, default);

        Assert.True(result.IsActive);
        Assert.True(driver.IsActive);
    }
}
