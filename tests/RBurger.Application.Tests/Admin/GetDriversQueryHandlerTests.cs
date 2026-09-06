using RBurger.Application.Admin.Drivers.Queries.GetDrivers;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class GetDriversQueryHandlerTests
{
    private static (GetDriversQueryHandler Handler, FakeDriverRepository Drivers, FakeOrderRepository Orders)
        BuildHandler()
    {
        var drivers = new FakeDriverRepository();
        var orders = new FakeOrderRepository();
        var handler = new GetDriversQueryHandler(drivers, orders);
        return (handler, drivers, orders);
    }

    [Fact]
    public async Task Handle_paginates_per_documented_defaults()
    {
        var (handler, drivers, _) = BuildHandler();
        for (var i = 0; i < 25; i++)
        {
            drivers.Drivers.Add(new Driver
            {
                Id = Guid.NewGuid(),
                FullName = $"Driver {i}",
                BranchId = 1,
                Vehicle = "bike",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }

        var result = await handler.Handle(new GetDriversQuery(), default); // page=1, pageSize=20 defaults

        Assert.Equal(20, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task Handle_computes_lifetime_deliveries_completed_count()
    {
        var (handler, drivers, orders) = BuildHandler();
        var driver = new Driver { Id = Guid.NewGuid(), FullName = "Karim Adel", BranchId = 1, Vehicle = "bike" };
        drivers.Drivers.Add(driver);

        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), DriverId = driver.Id, Stage = OrderStage.Delivered });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), DriverId = driver.Id, Stage = OrderStage.Delivered });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), DriverId = driver.Id, Stage = OrderStage.OnTheWay });

        var result = await handler.Handle(new GetDriversQuery(), default);

        Assert.Equal(2, result.Items.Single().DeliveriesCompleted); // only the Delivered ones count
    }

    [Fact]
    public async Task Handle_returns_zero_deliveries_completed_for_a_driver_with_no_orders()
    {
        var (handler, drivers, _) = BuildHandler();
        drivers.Drivers.Add(new Driver { Id = Guid.NewGuid(), FullName = "Karim Adel", BranchId = 1, Vehicle = "bike" });

        var result = await handler.Handle(new GetDriversQuery(), default);

        Assert.Equal(0, result.Items.Single().DeliveriesCompleted);
    }
}
