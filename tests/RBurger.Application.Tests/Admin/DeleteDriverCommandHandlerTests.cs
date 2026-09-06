using RBurger.Application.Admin.Drivers.Commands.DeleteDriver;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class DeleteDriverCommandHandlerTests
{
    private static (DeleteDriverCommandHandler Handler, FakeDriverRepository Drivers, FakeOrderRepository Orders)
        BuildHandler()
    {
        var drivers = new FakeDriverRepository();
        var orders = new FakeOrderRepository();
        var handler = new DeleteDriverCommandHandler(drivers, orders);
        return (handler, drivers, orders);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_driver_does_not_exist()
    {
        var (handler, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteDriverCommand { Id = Guid.NewGuid() }, default));
    }

    [Theory]
    [InlineData(OrderStage.Preparing)]
    [InlineData(OrderStage.OnTheWay)]
    public async Task Handle_throws_UnprocessableEntityException_when_driver_has_non_terminal_order(OrderStage stage)
    {
        var (handler, drivers, orders) = BuildHandler();
        var driver = new Driver { Id = Guid.NewGuid(), BranchId = 1 };
        drivers.Drivers.Add(driver);
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), DriverId = driver.Id, Stage = stage });

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new DeleteDriverCommand { Id = driver.Id }, default));
        Assert.Equal("DRIVER_HAS_NON_TERMINAL_ORDERS", ex.ErrorCode);
        Assert.Single(drivers.Drivers); // not deleted
    }

    // §7.6.3's non-terminal range is Stage 1-2 only; a Confirmed (Stage 0) order can never
    // actually carry a DriverId (see TryClaimForDriverAsync), but this proves the query itself
    // is scoped correctly rather than accidentally matching every stage.
    [Fact]
    public async Task Handle_allows_deletion_when_driver_only_has_terminal_delivered_orders()
    {
        var (handler, drivers, orders) = BuildHandler();
        var driver = new Driver { Id = Guid.NewGuid(), BranchId = 1 };
        drivers.Drivers.Add(driver);
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), DriverId = driver.Id, Stage = OrderStage.Delivered });

        await handler.Handle(new DeleteDriverCommand { Id = driver.Id }, default);

        Assert.Empty(drivers.Drivers);
    }

    [Fact]
    public async Task Handle_deletes_driver_when_no_orders_at_all()
    {
        var (handler, drivers, _) = BuildHandler();
        var driver = new Driver { Id = Guid.NewGuid(), BranchId = 1 };
        drivers.Drivers.Add(driver);

        await handler.Handle(new DeleteDriverCommand { Id = driver.Id }, default);

        Assert.Empty(drivers.Drivers);
    }
}
