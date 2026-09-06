using RBurger.Application.Common.Exceptions;
using RBurger.Application.Payments.Commands.PaymentWebhook;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Payments;

public class PaymentWebhookCommandHandlerTests
{
    private static Order Order(string paymentStatus = "pending") => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = 1,
        BranchId = 1,
        CustomerName = "Ahmed Sami",
        Total = 100,
        Payment = new Payment { Id = Guid.NewGuid(), Method = "card", Status = paymentStatus, Amount = 100 }
    };

    [Fact]
    public async Task Handle_throws_PaymentProviderNotConfiguredException_when_provider_unconfigured()
    {
        var orders = new FakeOrderRepository();
        var provider = new FakePaymentProvider { AlwaysThrow = true };
        var notifier = new FakeOrderRealtimeNotifier();
        var handler = new PaymentWebhookCommandHandler(orders, provider, notifier);

        await Assert.ThrowsAsync<PaymentProviderNotConfiguredException>(() =>
            handler.Handle(
                new PaymentWebhookCommand("TXN-1", Guid.NewGuid().ToString(), "success", 100, "{}", "sig"),
                default));
    }

    [Fact]
    public async Task Handle_success_sets_payment_captured_and_broadcasts_PaymentConfirmed()
    {
        var orders = new FakeOrderRepository();
        var order = Order();
        orders.AddedOrders.Add(order);
        var provider = new FakePaymentProvider { AlwaysThrow = false };
        var notifier = new FakeOrderRealtimeNotifier();
        var handler = new PaymentWebhookCommandHandler(orders, provider, notifier);

        var result = await handler.Handle(
            new PaymentWebhookCommand("TXN-1", order.Id.ToString(), "success", 100, "{}", "sig"), default);

        Assert.True(result.Received);
        Assert.Equal("captured", order.Payment!.Status);
        Assert.NotNull(order.Payment.PaidAt);
        var call = Assert.Single(notifier.PaymentConfirmedCalls);
        Assert.Equal(order.Id, call.OrderId);
        Assert.Equal("captured", call.Status);
    }

    [Fact]
    public async Task Handle_failure_sets_payment_failed_and_does_not_broadcast()
    {
        var orders = new FakeOrderRepository();
        var order = Order();
        orders.AddedOrders.Add(order);
        var provider = new FakePaymentProvider { AlwaysThrow = false };
        var notifier = new FakeOrderRealtimeNotifier();
        var handler = new PaymentWebhookCommandHandler(orders, provider, notifier);

        await handler.Handle(
            new PaymentWebhookCommand("TXN-1", order.Id.ToString(), "failure", 100, "{}", "sig"), default);

        Assert.Equal("failed", order.Payment!.Status);
        Assert.Empty(notifier.PaymentConfirmedCalls);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_for_invalid_orderReference()
    {
        var orders = new FakeOrderRepository();
        var provider = new FakePaymentProvider { AlwaysThrow = false };
        var notifier = new FakeOrderRealtimeNotifier();
        var handler = new PaymentWebhookCommandHandler(orders, provider, notifier);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new PaymentWebhookCommand("TXN-1", "not-a-guid", "success", 100, "{}", "sig"), default));
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_order_does_not_exist()
    {
        var orders = new FakeOrderRepository();
        var provider = new FakePaymentProvider { AlwaysThrow = false };
        var notifier = new FakeOrderRealtimeNotifier();
        var handler = new PaymentWebhookCommandHandler(orders, provider, notifier);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new PaymentWebhookCommand("TXN-1", Guid.NewGuid().ToString(), "success", 100, "{}", "sig"),
                default));
    }
}
