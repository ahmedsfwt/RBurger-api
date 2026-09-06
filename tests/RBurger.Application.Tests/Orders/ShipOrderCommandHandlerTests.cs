using RBurger.Application.Common.Exceptions;
using RBurger.Application.Orders.Commands.ShipOrder;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class ShipOrderCommandHandlerTests
{
    private static Order BuildOrder(OrderStage stage, Guid driverId) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = 4821,
        BranchId = 1,
        CustomerId = Guid.NewGuid(),
        DriverId = driverId,
        Stage = stage,
        CustomerName = "Ahmed Sami",
        Phone = "01012345678",
        Address = "Sohag, University street",
        Subtotal = 235m,
        DeliveryFee = 20m,
        Total = 255m,
        PaymentMethod = "cash"
    };

    private static (ShipOrderCommandHandler Handler, FakeOrderRepository Orders, FakeOrderRealtimeNotifier Notifier)
        BuildHandler()
    {
        var orders = new FakeOrderRepository();
        var notifier = new FakeOrderRealtimeNotifier();
        return (new ShipOrderCommandHandler(orders, notifier), orders, notifier);
    }

    [Fact]
    public async Task Should_advance_Stage_1_to_2_for_the_assigned_driver()
    {
        var driverId = Guid.NewGuid();
        var order = BuildOrder(OrderStage.Preparing, driverId);
        var (handler, orders, notifier) = BuildHandler();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(
            new ShipOrderCommand { OrderId = order.Id, DriverId = driverId }, CancellationToken.None);

        Assert.Equal(OrderStage.OnTheWay, result.Stage);
        Assert.Equal(OrderStage.OnTheWay, order.Stage);
        Assert.Single(orders.AddedStatusEvents);
        Assert.Single(notifier.StatusChangedCalls);
    }

    [Fact]
    public async Task Should_throw_NotFoundException_when_order_does_not_exist()
    {
        var (handler, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new ShipOrderCommand { OrderId = Guid.NewGuid(), DriverId = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_ForbiddenException_when_caller_is_not_the_assigned_driver()
    {
        var assignedDriverId = Guid.NewGuid();
        var otherDriverId = Guid.NewGuid();
        var order = BuildOrder(OrderStage.Preparing, assignedDriverId);
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new ShipOrderCommand { OrderId = order.Id, DriverId = otherDriverId }, CancellationToken.None));
    }

    [Theory]
    [InlineData(OrderStage.Confirmed)]
    [InlineData(OrderStage.OnTheWay)]
    [InlineData(OrderStage.Delivered)]
    public async Task Should_throw_UnprocessableEntityException_when_stage_is_not_Preparing(OrderStage stage)
    {
        var driverId = Guid.NewGuid();
        var order = BuildOrder(stage, driverId);
        var (handler, orders, notifier) = BuildHandler();
        orders.AddedOrders.Add(order);

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(
            new ShipOrderCommand { OrderId = order.Id, DriverId = driverId }, CancellationToken.None));

        Assert.Equal("INVALID_ORDER_STAGE", ex.ErrorCode);
        Assert.Empty(orders.AddedStatusEvents);
        Assert.Empty(notifier.StatusChangedCalls);
    }
}
