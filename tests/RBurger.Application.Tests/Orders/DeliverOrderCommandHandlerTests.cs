using RBurger.Application.Common.Exceptions;
using RBurger.Application.Orders.Commands.DeliverOrder;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class DeliverOrderCommandHandlerTests
{
    private static Order BuildOrder(OrderStage stage, Guid driverId, string paymentMethod = "cash", string paymentStatus = "pending") => new()
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
        PaymentMethod = paymentMethod,
        Payment = new Payment { Method = paymentMethod, Status = paymentStatus, Amount = 255m }
    };

    private static (DeliverOrderCommandHandler Handler, FakeOrderRepository Orders, FakeOrderRealtimeNotifier Notifier)
        BuildHandler()
    {
        var orders = new FakeOrderRepository();
        var notifier = new FakeOrderRealtimeNotifier();
        return (new DeliverOrderCommandHandler(orders, notifier), orders, notifier);
    }

    [Fact]
    public async Task Should_advance_Stage_2_to_3_for_the_assigned_driver()
    {
        var driverId = Guid.NewGuid();
        var order = BuildOrder(OrderStage.OnTheWay, driverId);
        var (handler, orders, notifier) = BuildHandler();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(
            new DeliverOrderCommand { OrderId = order.Id, DriverId = driverId }, CancellationToken.None);

        Assert.Equal(OrderStage.Delivered, result.Stage);
        Assert.Equal(OrderStage.Delivered, order.Stage);
        Assert.Single(orders.AddedStatusEvents);
        Assert.Single(notifier.StatusChangedCalls);
    }

    [Fact]
    public async Task Should_throw_NotFoundException_when_order_does_not_exist()
    {
        var (handler, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new DeliverOrderCommand { OrderId = Guid.NewGuid(), DriverId = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_ForbiddenException_when_caller_is_not_the_assigned_driver()
    {
        var assignedDriverId = Guid.NewGuid();
        var otherDriverId = Guid.NewGuid();
        var order = BuildOrder(OrderStage.OnTheWay, assignedDriverId);
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new DeliverOrderCommand { OrderId = order.Id, DriverId = otherDriverId }, CancellationToken.None));
    }

    [Theory]
    [InlineData(OrderStage.Confirmed)]
    [InlineData(OrderStage.Preparing)]
    [InlineData(OrderStage.Delivered)]
    public async Task Should_throw_UnprocessableEntityException_when_stage_is_not_OnTheWay(OrderStage stage)
    {
        var driverId = Guid.NewGuid();
        var order = BuildOrder(stage, driverId);
        var (handler, orders, notifier) = BuildHandler();
        orders.AddedOrders.Add(order);

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(
            new DeliverOrderCommand { OrderId = order.Id, DriverId = driverId }, CancellationToken.None));

        Assert.Equal("INVALID_ORDER_STAGE", ex.ErrorCode);
        Assert.Empty(orders.AddedStatusEvents);
        Assert.Empty(notifier.StatusChangedCalls);
    }

    [Fact]
    public async Task Should_auto_capture_cash_payment_on_successful_delivery()
    {
        // §9.1: "Cash on Delivery ... pending -> captured, auto-set by the
        // /driver/orders/{id}/deliver endpoint the moment the driver confirms delivery."
        var driverId = Guid.NewGuid();
        var order = BuildOrder(OrderStage.OnTheWay, driverId, paymentMethod: "cash", paymentStatus: "pending");
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);

        await handler.Handle(new DeliverOrderCommand { OrderId = order.Id, DriverId = driverId }, CancellationToken.None);

        Assert.Equal("captured", order.Payment!.Status);
    }

    [Fact]
    public async Task Should_not_touch_card_payment_status_on_delivery()
    {
        // §9.1: card payments follow pending -> authorized -> captured via the gateway
        // webhook (§7.7/§9.3), not the /deliver endpoint - only cash is auto-captured here.
        var driverId = Guid.NewGuid();
        var order = BuildOrder(OrderStage.OnTheWay, driverId, paymentMethod: "card", paymentStatus: "authorized");
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);

        await handler.Handle(new DeliverOrderCommand { OrderId = order.Id, DriverId = driverId }, CancellationToken.None);

        Assert.Equal("authorized", order.Payment!.Status);
    }
}
