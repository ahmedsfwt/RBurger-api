using RBurger.Application.Common.Exceptions;
using RBurger.Application.Orders.Queries.GetDriverNewOrders;
using RBurger.Application.Tests.Authentication;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class GetDriverNewOrdersQueryHandlerTests
{
    private static Driver BuildDriver(Guid id, int branchId) => new()
    {
        Id = id,
        FullName = "Karim Adel",
        Phone = "01099988877",
        PasswordHash = "hashed",
        Vehicle = "bike",
        BranchId = branchId,
        CreatedByAdminId = Guid.NewGuid(),
        IsActive = true
    };

    private static Order BuildOrder(
        int branchId, OrderStage stage, Guid? driverId = null) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = 4821,
        BranchId = branchId,
        CustomerId = Guid.NewGuid(),
        DriverId = driverId,
        Stage = stage,
        CustomerName = "Ahmed Sami",
        Phone = "01012345678",
        Address = "Sohag, University street",
        Notes = "No pickles please",
        Subtotal = 235m,
        DeliveryFee = 20m,
        Total = 255m,
        PaymentMethod = "cash"
    };

    [Fact]
    public async Task Should_return_only_unclaimed_Stage_0_or_1_orders_at_the_drivers_own_branch()
    {
        var driverId = Guid.NewGuid();
        var drivers = new FakeDriverRepository();
        drivers.Drivers.Add(BuildDriver(driverId, branchId: 1));

        var orders = new FakeOrderRepository();
        var matchingConfirmed = BuildOrder(branchId: 1, stage: OrderStage.Confirmed);
        var matchingPreparing = BuildOrder(branchId: 1, stage: OrderStage.Preparing);
        var wrongBranch = BuildOrder(branchId: 2, stage: OrderStage.Confirmed);
        var alreadyClaimed = BuildOrder(branchId: 1, stage: OrderStage.Preparing, driverId: Guid.NewGuid());
        var wrongStage = BuildOrder(branchId: 1, stage: OrderStage.OnTheWay);
        orders.AddedOrders.AddRange(new[]
        {
            matchingConfirmed, matchingPreparing, wrongBranch, alreadyClaimed, wrongStage
        });

        var handler = new GetDriverNewOrdersQueryHandler(drivers, orders);

        var result = await handler.Handle(new GetDriverNewOrdersQuery { DriverId = driverId }, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.OrderId == matchingConfirmed.Id);
        Assert.Contains(result, r => r.OrderId == matchingPreparing.Id);
        Assert.DoesNotContain(result, r => r.OrderId == wrongBranch.Id);
        Assert.DoesNotContain(result, r => r.OrderId == alreadyClaimed.Id);
        Assert.DoesNotContain(result, r => r.OrderId == wrongStage.Id);
    }

    [Fact]
    public async Task Should_throw_NotFoundException_when_driver_does_not_exist()
    {
        var drivers = new FakeDriverRepository();
        var orders = new FakeOrderRepository();
        var handler = new GetDriverNewOrdersQueryHandler(drivers, orders);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new GetDriverNewOrdersQuery { DriverId = Guid.NewGuid() }, CancellationToken.None));
    }
}
