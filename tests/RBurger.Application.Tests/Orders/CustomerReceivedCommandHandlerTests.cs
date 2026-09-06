using RBurger.Application.Common.Exceptions;
using RBurger.Application.Orders.Commands.CustomerReceived;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class CustomerReceivedCommandHandlerTests
{
    private static Order BuildOrder(Guid ownerCustomerId, OrderStage stage, DateTime? customerReceivedAt = null)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = 4821,
            BranchId = 1,
            CustomerId = ownerCustomerId,
            Stage = stage,
            CustomerName = "Ahmed Sami",
            Phone = "01012345678",
            Address = "Sohag, University street",
            Subtotal = 235m,
            DeliveryFee = 20m,
            Total = 255m,
            PaymentMethod = "cash",
            CustomerReceivedAt = customerReceivedAt
        };
    }

    private static CustomerReceivedCommandHandler BuildHandler(
        FakeOrderRepository repository, FakeOrderRealtimeNotifier? realtimeNotifier = null)
    {
        return new CustomerReceivedCommandHandler(repository, realtimeNotifier ?? new FakeOrderRealtimeNotifier());
    }

    [Fact]
    public async Task Should_set_CustomerReceivedAt_when_order_is_owned_and_delivered()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId, OrderStage.Delivered);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = BuildHandler(repository);
        var before = DateTime.UtcNow;

        var result = await handler.Handle(
            new CustomerReceivedCommand { OrderId = order.Id, CustomerId = ownerId },
            CancellationToken.None);

        var after = DateTime.UtcNow;
        Assert.Equal(order.Id, result.OrderId);
        Assert.NotNull(order.CustomerReceivedAt);
        Assert.Equal(result.CustomerReceivedAt, order.CustomerReceivedAt);
        // CustomerReceivedAt is populated using UTC (test case 6).
        Assert.Equal(DateTimeKind.Utc, result.CustomerReceivedAt.Kind);
        Assert.InRange(result.CustomerReceivedAt, before, after);
    }

    [Fact]
    public async Task Should_throw_NotFoundException_when_order_does_not_exist()
    {
        var repository = new FakeOrderRepository();
        var handler = BuildHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new CustomerReceivedCommand { OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid() },
            CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_ForbiddenException_when_requesting_customer_is_not_the_owner()
    {
        var ownerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var order = BuildOrder(ownerId, OrderStage.Delivered);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = BuildHandler(repository);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new CustomerReceivedCommand { OrderId = order.Id, CustomerId = otherCustomerId },
            CancellationToken.None));
    }

    [Theory]
    [InlineData(OrderStage.Confirmed)]
    [InlineData(OrderStage.Preparing)]
    [InlineData(OrderStage.OnTheWay)]
    public async Task Should_throw_UnprocessableEntityException_when_stage_is_not_delivered(OrderStage stage)
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId, stage);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = BuildHandler(repository);

        await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(
            new CustomerReceivedCommand { OrderId = order.Id, CustomerId = ownerId },
            CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_ConflictException_when_CustomerReceivedAt_is_already_set()
    {
        var ownerId = Guid.NewGuid();
        var alreadySetAt = DateTime.UtcNow.AddMinutes(-5);
        var order = BuildOrder(ownerId, OrderStage.Delivered, alreadySetAt);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = BuildHandler(repository);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CustomerReceivedCommand { OrderId = order.Id, CustomerId = ownerId },
            CancellationToken.None));

        // Confirms the handler did not overwrite the already-set value before throwing.
        Assert.Equal(alreadySetAt, order.CustomerReceivedAt);
    }

    [Fact]
    public async Task Should_not_create_any_OrderStatusEvent_on_success()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId, OrderStage.Delivered);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = BuildHandler(repository);

        await handler.Handle(
            new CustomerReceivedCommand { OrderId = order.Id, CustomerId = ownerId },
            CancellationToken.None);

        // §7.4's prose does not request an OrderStatusEvent for this endpoint, and Stage
        // does not change - approved Day 5 decision, unchanged by Day 8. See Phase 0 report
        // §3.D. Persisted audit-trail semantics stay separate from the realtime broadcast
        // added below by Day 8 approved decision #3.
        Assert.Empty(order.OrderStatusEvents);
    }

    // ---- Day 8 approved decision #3 ----

    [Fact]
    public async Task Should_broadcast_OrderStatusChanged_after_a_successful_customer_received()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId, OrderStage.Delivered);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var realtimeNotifier = new FakeOrderRealtimeNotifier();
        var handler = BuildHandler(repository, realtimeNotifier);

        var result = await handler.Handle(
            new CustomerReceivedCommand { OrderId = order.Id, CustomerId = ownerId },
            CancellationToken.None);

        var call = Assert.Single(realtimeNotifier.StatusChangedCalls);
        Assert.Equal(order.Id, call.OrderId);
        // §7.4/§6.3: Stage does not change - still Delivered=3.
        Assert.Equal(OrderStage.Delivered, call.Stage);
        Assert.Equal(result.CustomerReceivedAt, call.Timestamp);
        Assert.Equal("customer", call.TriggeredBy);
    }

    [Theory]
    [InlineData(OrderStage.Confirmed)]
    [InlineData(OrderStage.Preparing)]
    [InlineData(OrderStage.OnTheWay)]
    public async Task Should_not_broadcast_when_stage_precondition_fails(OrderStage stage)
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId, stage);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var realtimeNotifier = new FakeOrderRealtimeNotifier();
        var handler = BuildHandler(repository, realtimeNotifier);

        await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(
            new CustomerReceivedCommand { OrderId = order.Id, CustomerId = ownerId },
            CancellationToken.None));

        // §8.3: broadcast only happens after a successful DB write - never on a failed call.
        Assert.Empty(realtimeNotifier.StatusChangedCalls);
    }

    [Fact]
    public async Task Should_not_broadcast_when_already_received()
    {
        var ownerId = Guid.NewGuid();
        var alreadySetAt = DateTime.UtcNow.AddMinutes(-5);
        var order = BuildOrder(ownerId, OrderStage.Delivered, alreadySetAt);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var realtimeNotifier = new FakeOrderRealtimeNotifier();
        var handler = BuildHandler(repository, realtimeNotifier);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CustomerReceivedCommand { OrderId = order.Id, CustomerId = ownerId },
            CancellationToken.None));

        Assert.Empty(realtimeNotifier.StatusChangedCalls);
    }

    [Fact]
    public void Handler_constructor_depends_only_on_IOrderRepository_and_IOrderRealtimeNotifier()
    {
        // Day 8 approved decision #3 reverses the Day 5 decision this test used to assert
        // (see git history) - CustomerReceivedCommandHandler now depends on the same
        // Application-layer IOrderRealtimeNotifier abstraction as the §7.5 driver handlers,
        // never on a concrete SignalR/Hub type (Clean Architecture rule, unchanged).
        var constructors = typeof(CustomerReceivedCommandHandler).GetConstructors();
        var parameterTypes = Assert.Single(constructors).GetParameters()
            .Select(p => p.ParameterType.Name)
            .ToList();

        Assert.Equal(2, parameterTypes.Count);
        Assert.Contains("IOrderRepository", parameterTypes);
        Assert.Contains("IOrderRealtimeNotifier", parameterTypes);
        Assert.DoesNotContain(parameterTypes, t => t.Contains("Hub") || t.Contains("SignalR"));
    }
}
