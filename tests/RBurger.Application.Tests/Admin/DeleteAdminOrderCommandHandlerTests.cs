using RBurger.Application.Admin.Orders.Commands.DeleteAdminOrder;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class DeleteAdminOrderCommandHandlerTests
{
    private static Order OrderWithPayment(string paymentStatus, string paymentMethod, OrderStage stage = OrderStage.Delivered)
    {
        var orderId = Guid.NewGuid();
        return new Order
        {
            Id = orderId,
            OrderNumber = 1,
            BranchId = 1,
            Stage = stage,
            CustomerName = "Ahmed Sami",
            Total = 100,
            IsCancelled = false,
            Payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Method = paymentMethod,
                Status = paymentStatus,
                Amount = 100
            }
        };
    }

    private static Order OrderWithoutPayment(OrderStage stage = OrderStage.Confirmed)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = 2,
            BranchId = 1,
            Stage = stage,
            CustomerName = "Ahmed Sami",
            Total = 100,
            IsCancelled = false
        };
    }

    private static (DeleteAdminOrderCommandHandler Handler, FakeOrderRepository Orders, FakePaymentProvider Provider)
        BuildHandler(bool providerConfigured = false, bool providerRefundSucceeds = true)
    {
        var orders = new FakeOrderRepository();
        var provider = new FakePaymentProvider { AlwaysThrow = !providerConfigured };
        var handler = new DeleteAdminOrderCommandHandler(orders, provider);
        return (handler, orders, provider);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_order_does_not_exist()
    {
        var (handler, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteAdminOrderCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_throws_UnprocessableEntityException_when_order_already_cancelled()
    {
        var (handler, orders, _) = BuildHandler();
        var order = OrderWithoutPayment();
        order.IsCancelled = true;
        order.CancelledAt = DateTime.UtcNow.AddMinutes(-10);
        orders.AddedOrders.Add(order);

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new DeleteAdminOrderCommand(order.Id, Guid.NewGuid()), default));

        Assert.Equal("ORDER_ALREADY_CANCELLED", ex.ErrorCode);
    }

    // ---- CASE A: pending/authorized (and payment-less/failed) - §9.4 "marked cancelled with
    // no money movement" ----

    [Theory]
    [InlineData("pending")]
    [InlineData("authorized")]
    public async Task Handle_pending_or_authorized_cancels_with_no_money_movement(string status)
    {
        var (handler, orders, provider) = BuildHandler();
        var order = OrderWithPayment(status, "card");
        orders.AddedOrders.Add(order);
        var adminId = Guid.NewGuid();

        await handler.Handle(new DeleteAdminOrderCommand(order.Id, adminId), default);

        Assert.True(order.IsCancelled);
        Assert.NotNull(order.CancelledAt);
        Assert.Equal(status, order.Payment!.Status); // untouched - no money movement
        Assert.Empty(provider.RefundCalls); // gateway never called for pending/authorized

        var statusEvent = Assert.Single(orders.AddedStatusEvents);
        Assert.Equal(order.Id, statusEvent.OrderId);
        Assert.Equal("admin", statusEvent.TriggeredBy);
        Assert.Equal(adminId, statusEvent.ActorId);
    }

    [Fact]
    public async Task Handle_order_with_no_payment_row_cancels_with_no_money_movement()
    {
        var (handler, orders, provider) = BuildHandler();
        var order = OrderWithoutPayment();
        orders.AddedOrders.Add(order);

        await handler.Handle(new DeleteAdminOrderCommand(order.Id, Guid.NewGuid()), default);

        Assert.True(order.IsCancelled);
        Assert.NotNull(order.CancelledAt);
        Assert.Empty(provider.RefundCalls);
    }

    // ---- CASE B: captured + card - §9.4 gateway refund ----

    [Fact]
    public async Task Handle_captured_card_with_unconfigured_provider_throws_and_does_not_mutate_state()
    {
        var (handler, orders, provider) = BuildHandler(providerConfigured: false);
        var order = OrderWithPayment("captured", "card");
        orders.AddedOrders.Add(order);

        await Assert.ThrowsAsync<PaymentProviderNotConfiguredException>(() =>
            handler.Handle(new DeleteAdminOrderCommand(order.Id, Guid.NewGuid()), default));

        // Atomicity requirement: nothing about the order/payment was touched when the refund
        // could not even be attempted.
        Assert.Equal("captured", order.Payment!.Status);
        Assert.False(order.IsCancelled);
        Assert.Null(order.CancelledAt);
        Assert.Empty(orders.AddedStatusEvents);
    }

    [Fact]
    public async Task Handle_captured_card_with_configured_provider_refunds_and_cancels_the_order()
    {
        var (handler, orders, provider) = BuildHandler(providerConfigured: true);
        var order = OrderWithPayment("captured", "card", OrderStage.Delivered);
        orders.AddedOrders.Add(order);
        var adminId = Guid.NewGuid();

        await handler.Handle(new DeleteAdminOrderCommand(order.Id, adminId), default);

        Assert.Equal("refunded", order.Payment!.Status);
        Assert.True(order.IsCancelled);
        Assert.NotNull(order.CancelledAt);
        Assert.Single(provider.RefundCalls);

        var statusEvent = Assert.Single(orders.AddedStatusEvents);
        Assert.Equal(order.Id, statusEvent.OrderId);
        Assert.Equal(OrderStage.Delivered, statusEvent.Stage); // unchanged Stage, per the handler's XML comment
        Assert.Equal("admin", statusEvent.TriggeredBy);
        Assert.Equal(adminId, statusEvent.ActorId);
    }

    [Fact]
    public async Task Handle_captured_card_with_declined_refund_does_not_cancel_the_order()
    {
        var orders = new FakeOrderRepository();
        var provider = new FakePaymentProvider { AlwaysThrow = false, NextRefundSucceeds = false };
        var handler = new DeleteAdminOrderCommandHandler(orders, provider);
        var order = OrderWithPayment("captured", "card");
        orders.AddedOrders.Add(order);

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new DeleteAdminOrderCommand(order.Id, Guid.NewGuid()), default));

        Assert.Equal("REFUND_FAILED", ex.ErrorCode);

        // Payment/order state must remain exactly as it was before the declined refund attempt.
        Assert.Equal("captured", order.Payment!.Status);
        Assert.False(order.IsCancelled);
        Assert.Null(order.CancelledAt);
        Assert.Empty(orders.AddedStatusEvents);
    }

    // ---- CASE C: captured + cash - §9.4 manual refund note ----

    [Fact]
    public async Task Handle_captured_cash_records_manual_refund_note_and_cancels_the_order()
    {
        var (handler, orders, provider) = BuildHandler();
        var order = OrderWithPayment("captured", "cash");
        orders.AddedOrders.Add(order);
        var adminId = Guid.NewGuid();

        await handler.Handle(new DeleteAdminOrderCommand(order.Id, adminId), default);

        Assert.NotNull(order.Payment!.CashRefundNote);
        Assert.Contains(adminId.ToString(), order.Payment.CashRefundNote);
        Assert.NotNull(order.Payment.CashRefundNotedAt);

        // §9.4 never states Payments.Status changes for the cash branch (unlike the card
        // branch) - cash was genuinely captured, no gateway movement occurs.
        Assert.Equal("captured", order.Payment.Status);

        Assert.True(order.IsCancelled);
        Assert.NotNull(order.CancelledAt);
        Assert.Empty(provider.RefundCalls); // no gateway call for the cash branch

        var statusEvent = Assert.Single(orders.AddedStatusEvents);
        Assert.Equal(order.Id, statusEvent.OrderId);
        Assert.Equal("admin", statusEvent.TriggeredBy);
        Assert.Equal(adminId, statusEvent.ActorId);
    }

    [Fact]
    public async Task Handle_captured_cash_persists_the_refund_note_and_timestamp_together()
    {
        var (handler, orders, _) = BuildHandler();
        var order = OrderWithPayment("captured", "cash");
        orders.AddedOrders.Add(order);
        var before = DateTime.UtcNow;

        await handler.Handle(new DeleteAdminOrderCommand(order.Id, Guid.NewGuid()), default);

        Assert.True(order.Payment!.CashRefundNotedAt >= before);
        Assert.True(order.CancelledAt >= before);
    }
}
