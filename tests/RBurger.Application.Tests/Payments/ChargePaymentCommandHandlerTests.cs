using RBurger.Application.Common.Exceptions;
using RBurger.Application.Payments.Commands.ChargePayment;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Payments;

public class ChargePaymentCommandHandlerTests
{
    private static Order Order(Guid customerId, string paymentMethod = "card") => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = 1,
        BranchId = 1,
        CustomerId = customerId,
        CustomerName = "Ahmed Sami",
        Total = 100,
        Payment = new Payment { Id = Guid.NewGuid(), Method = paymentMethod, Status = "pending", Amount = 100 }
    };

    [Fact]
    public async Task Handle_throws_NotFoundException_when_order_missing()
    {
        var orders = new FakeOrderRepository();
        var provider = new FakePaymentProvider();
        var handler = new ChargePaymentCommandHandler(orders, provider);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ChargePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), "key-1"), default));
    }

    [Fact]
    public async Task Handle_throws_ForbiddenException_when_customer_does_not_own_order()
    {
        var orders = new FakeOrderRepository();
        var order = Order(Guid.NewGuid());
        orders.AddedOrders.Add(order);
        var provider = new FakePaymentProvider();
        var handler = new ChargePaymentCommandHandler(orders, provider);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ChargePaymentCommand(order.Id, Guid.NewGuid(), "key-1"), default));
    }

    [Fact]
    public async Task Handle_throws_UnprocessableEntityException_when_payment_method_is_not_card()
    {
        var customerId = Guid.NewGuid();
        var orders = new FakeOrderRepository();
        var order = Order(customerId, "cash");
        orders.AddedOrders.Add(order);
        var provider = new FakePaymentProvider();
        var handler = new ChargePaymentCommandHandler(orders, provider);

        await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new ChargePaymentCommand(order.Id, customerId, "key-1"), default));
    }

    [Fact]
    public async Task Handle_throws_PaymentProviderNotConfiguredException_when_provider_unconfigured()
    {
        var customerId = Guid.NewGuid();
        var orders = new FakeOrderRepository();
        var order = Order(customerId);
        orders.AddedOrders.Add(order);
        var provider = new FakePaymentProvider { AlwaysThrow = true };
        var handler = new ChargePaymentCommandHandler(orders, provider);

        await Assert.ThrowsAsync<PaymentProviderNotConfiguredException>(() =>
            handler.Handle(new ChargePaymentCommand(order.Id, customerId, "key-1"), default));

        Assert.Equal("pending", order.Payment!.Status); // unchanged
    }

    [Fact]
    public async Task Handle_returns_session_details_when_provider_configured()
    {
        var customerId = Guid.NewGuid();
        var orders = new FakeOrderRepository();
        var order = Order(customerId);
        orders.AddedOrders.Add(order);
        var provider = new FakePaymentProvider { AlwaysThrow = false };
        var handler = new ChargePaymentCommandHandler(orders, provider);

        var result = await handler.Handle(new ChargePaymentCommand(order.Id, customerId, "key-1"), default);

        Assert.Equal(order.Payment!.Id, result.PaymentId);
        Assert.Equal("pending", result.Status); // §9.3: status stays pending at session-creation time
        Assert.False(string.IsNullOrEmpty(result.RedirectUrl));
        Assert.Single(provider.ChargeCalls);
    }
}
