using System.Text.Json;
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

    private static string Payload(
        object orderReference, bool success, bool pending = false,
        long amountCents = 10000, string type = "TRANSACTION")
        => JsonSerializer.Serialize(new
        {
            type,
            obj = new
            {
                id = 123456,
                success,
                pending,
                is_voided = false,
                is_refunded = false,
                amount_cents = amountCents,
                currency = "EGP",
                order = new { id = 99, merchant_order_id = orderReference.ToString() }
            }
        });

    private static (PaymentWebhookCommandHandler Handler, FakeOrderRepository Orders,
        FakePaymentProvider Provider, FakeOrderRealtimeNotifier Notifier) Build(bool alwaysThrow = false)
    {
        var orders = new FakeOrderRepository();
        var provider = new FakePaymentProvider { AlwaysThrow = alwaysThrow };
        var notifier = new FakeOrderRealtimeNotifier();
        return (new PaymentWebhookCommandHandler(orders, provider, notifier), orders, provider, notifier);
    }

    [Fact]
    public async Task Handle_throws_PaymentProviderNotConfiguredException_when_provider_unconfigured()
    {
        var (handler, _, _, _) = Build(alwaysThrow: true);

        await Assert.ThrowsAsync<PaymentProviderNotConfiguredException>(() =>
            handler.Handle(new PaymentWebhookCommand(Payload(Guid.NewGuid(), true), "sig"), default));
    }

    [Fact]
    public async Task Handle_throws_InvalidWebhookSignatureException_when_hmac_missing()
    {
        var (handler, _, _, _) = Build();

        await Assert.ThrowsAsync<InvalidWebhookSignatureException>(() =>
            handler.Handle(new PaymentWebhookCommand(Payload(Guid.NewGuid(), true), null), default));
    }

    [Fact]
    public async Task Handle_throws_InvalidWebhookSignatureException_when_signature_invalid()
    {
        var (handler, orders, provider, notifier) = Build();
        provider.SignatureValid = false;
        var order = Order();
        orders.AddedOrders.Add(order);

        await Assert.ThrowsAsync<InvalidWebhookSignatureException>(() =>
            handler.Handle(new PaymentWebhookCommand(Payload(order.Id, true), "bad"), default));

        Assert.Equal("pending", order.Payment!.Status);
        Assert.Empty(notifier.PaymentConfirmedCalls);
    }

    [Fact]
    public async Task Handle_success_sets_captured_stores_transaction_and_broadcasts()
    {
        var (handler, orders, _, notifier) = Build();
        var order = Order();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(new PaymentWebhookCommand(Payload(order.Id, true), "sig"), default);

        Assert.True(result.Received);
        Assert.Equal("captured", order.Payment!.Status);
        Assert.Equal("paymob", order.Payment.GatewayProvider);
        Assert.Equal("123456", order.Payment.GatewayTransactionId);
        Assert.NotNull(order.Payment.PaidAt);
        var call = Assert.Single(notifier.PaymentConfirmedCalls);
        Assert.Equal(order.Id, call.OrderId);
        Assert.Equal("captured", call.Status);
    }

    [Fact]
    public async Task Handle_failure_sets_failed_and_does_not_broadcast()
    {
        var (handler, orders, _, notifier) = Build();
        var order = Order();
        orders.AddedOrders.Add(order);

        await handler.Handle(new PaymentWebhookCommand(Payload(order.Id, false), "sig"), default);

        Assert.Equal("failed", order.Payment!.Status);
        Assert.Empty(notifier.PaymentConfirmedCalls);
    }

    [Fact]
    public async Task Handle_pending_transaction_changes_nothing()
    {
        var (handler, orders, _, notifier) = Build();
        var order = Order();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(
            new PaymentWebhookCommand(Payload(order.Id, false, pending: true), "sig"), default);

        Assert.True(result.Received);
        Assert.Equal("pending", order.Payment!.Status);
        Assert.Empty(notifier.PaymentConfirmedCalls);
    }

    [Fact]
    public async Task Handle_non_transaction_callback_changes_nothing()
    {
        var (handler, orders, _, _) = Build();
        var order = Order();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(
            new PaymentWebhookCommand(Payload(order.Id, true, type: "TOKEN"), "sig"), default);

        Assert.True(result.Received);
        Assert.Equal("pending", order.Payment!.Status);
    }

    [Fact]
    public async Task Handle_already_captured_is_ignored_and_not_rebroadcast()
    {
        var (handler, orders, _, notifier) = Build();
        var order = Order("captured");
        orders.AddedOrders.Add(order);

        // A late failure webhook must not downgrade a paid order.
        await handler.Handle(new PaymentWebhookCommand(Payload(order.Id, false), "sig"), default);

        Assert.Equal("captured", order.Payment!.Status);
        Assert.Empty(notifier.PaymentConfirmedCalls);
    }

    [Fact]
    public async Task Handle_throws_UnprocessableEntityException_when_amount_mismatch()
    {
        var (handler, orders, _, notifier) = Build();
        var order = Order();
        orders.AddedOrders.Add(order);

        await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new PaymentWebhookCommand(Payload(order.Id, true, amountCents: 5000), "sig"), default));

        Assert.Equal("pending", order.Payment!.Status);
        Assert.Empty(notifier.PaymentConfirmedCalls);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_for_invalid_merchant_order_id()
    {
        var (handler, _, _, _) = Build();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new PaymentWebhookCommand(Payload("not-a-guid", true), "sig"), default));
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_order_does_not_exist()
    {
        var (handler, _, _, _) = Build();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new PaymentWebhookCommand(Payload(Guid.NewGuid(), true), "sig"), default));
    }
}