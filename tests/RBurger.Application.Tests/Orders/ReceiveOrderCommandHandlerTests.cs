using RBurger.Application.Common.Exceptions;
using RBurger.Application.Orders.Commands.ReceiveOrder;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class ReceiveOrderCommandHandlerTests
{
    private static Order BuildOrder(OrderStage stage, Guid? driverId = null) => new()
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

    private static (ReceiveOrderCommandHandler Handler, FakeOrderRepository Orders, FakeOrderRealtimeNotifier Notifier)
        BuildHandler()
    {
        var orders = new FakeOrderRepository();
        var notifier = new FakeOrderRealtimeNotifier();
        return (new ReceiveOrderCommandHandler(orders, notifier), orders, notifier);
    }

    [Fact]
    public async Task Should_assign_driver_and_advance_Stage_0_to_1_when_unclaimed()
    {
        var driverId = Guid.NewGuid();
        var order = BuildOrder(OrderStage.Confirmed);
        var (handler, orders, notifier) = BuildHandler();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(
            new ReceiveOrderCommand { OrderId = order.Id, DriverId = driverId }, CancellationToken.None);

        Assert.Equal(driverId, result.DriverId);
        Assert.Equal(OrderStage.Preparing, result.Stage);
        Assert.Equal(driverId, order.DriverId);
        Assert.Equal(OrderStage.Preparing, order.Stage);
        Assert.Single(orders.AddedStatusEvents);
        Assert.Equal("driver", orders.AddedStatusEvents[0].TriggeredBy);
        Assert.Equal(driverId, orders.AddedStatusEvents[0].ActorId);
        Assert.Single(notifier.StatusChangedCalls);
        Assert.Equal("driver", notifier.StatusChangedCalls[0].TriggeredBy);
    }

    [Fact]
    public async Task Should_not_change_Stage_when_already_Preparing()
    {
        var driverId = Guid.NewGuid();
        var order = BuildOrder(OrderStage.Preparing);
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(
            new ReceiveOrderCommand { OrderId = order.Id, DriverId = driverId }, CancellationToken.None);

        Assert.Equal(OrderStage.Preparing, result.Stage);
    }

    [Fact]
    public async Task Should_throw_NotFoundException_when_order_does_not_exist()
    {
        var (handler, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new ReceiveOrderCommand { OrderId = Guid.NewGuid(), DriverId = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_ConflictException_when_order_already_received_by_another_driver()
    {
        var firstDriverId = Guid.NewGuid();
        var secondDriverId = Guid.NewGuid();
        var order = BuildOrder(OrderStage.Preparing, driverId: firstDriverId);
        var (handler, orders, notifier) = BuildHandler();
        orders.AddedOrders.Add(order);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new ReceiveOrderCommand { OrderId = order.Id, DriverId = secondDriverId }, CancellationToken.None));

        // DriverId must remain the first driver's - the race loser must not overwrite it.
        Assert.Equal(firstDriverId, order.DriverId);
        Assert.Empty(orders.AddedStatusEvents);
        Assert.Empty(notifier.StatusChangedCalls);
    }
}
