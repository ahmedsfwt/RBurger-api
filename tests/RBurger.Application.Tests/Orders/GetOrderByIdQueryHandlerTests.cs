using RBurger.Application.Common.Exceptions;
using RBurger.Application.Orders.Queries.GetOrderById;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class GetOrderByIdQueryHandlerTests
{
    private static Branch SohagBranch() => new()
    {
        Id = 1,
        NameAr = "سوهاج",
        NameEn = "Sohag",
        DeliveryFee = 20m,
        EtaMinMinutes = 25,
        EtaMaxMinutes = 35,
        HotlinePhones = "01000000000",
        IsActive = true
    };

    private static Order BuildOrder(Guid ownerCustomerId)
    {
        var orderId = Guid.NewGuid();
        return new Order
        {
            Id = orderId,
            OrderNumber = 4821,
            BranchId = 1,
            Branch = SohagBranch(),
            CustomerId = ownerCustomerId,
            Stage = OrderStage.OnTheWay,
            CustomerName = "Ahmed Sami",
            Phone = "01012345678",
            Address = "Sohag, University street",
            Subtotal = 235m,
            DeliveryFee = 20m,
            Total = 255m,
            PaymentMethod = "cash",
            OrderItems = new List<OrderItem>
            {
                new() { NameAr = "أورجينال", NameEn = "Original", Quantity = 2, UnitPrice = 90m }
            },
            Payment = new Payment { Method = "cash", Status = "pending", Amount = 255m }
        };
    }

    [Fact]
    public async Task Should_return_order_when_requesting_customer_is_the_owner()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        var result = await handler.Handle(
            new GetOrderByIdQuery { OrderId = order.Id, RequestingCustomerId = ownerId },
            CancellationToken.None);

        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(order.OrderNumber, result.OrderNumber);
        Assert.Null(result.EtaSecondsRemaining); // approved decision #6, still blocked (Day 8)
        Assert.Null(result.Review); // no review on this order yet
    }

    // ---- Day 8 approved decision #2 ----

    [Fact]
    public async Task Should_populate_Review_using_the_same_shape_as_the_review_endpoint_when_one_exists()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        var review = new Review
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            CustomerId = ownerId,
            Rating = 5,
            Comment = "الاكل كان سخن ووصل بسرعة",
            CreatedAt = DateTime.UtcNow
        };
        order.Review = review;
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        var result = await handler.Handle(
            new GetOrderByIdQuery { OrderId = order.Id, RequestingCustomerId = ownerId },
            CancellationToken.None);

        Assert.NotNull(result.Review);
        Assert.Equal(review.Id, result.Review!.ReviewId);
        Assert.Equal(order.Id, result.Review.OrderId);
        Assert.Equal(5, result.Review.Rating);
        Assert.Equal(review.Comment, result.Review.Comment);
        Assert.Equal(review.CreatedAt, result.Review.CreatedAt);
    }

    [Fact]
    public async Task Should_throw_ForbiddenException_when_requesting_customer_is_not_the_owner()
    {
        var ownerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new GetOrderByIdQuery { OrderId = order.Id, RequestingCustomerId = otherCustomerId },
            CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_NotFoundException_when_order_does_not_exist()
    {
        var repository = new FakeOrderRepository();
        var handler = new GetOrderByIdQueryHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new GetOrderByIdQuery { OrderId = Guid.NewGuid(), RequestingCustomerId = Guid.NewGuid() },
            CancellationToken.None));
    }

    // ---- Day 7: Driver branch (§7.4 "Customer JWT (own order) or Driver JWT (assigned order)") ----

    [Fact]
    public async Task Should_return_order_when_requesting_driver_is_the_assigned_driver()
    {
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        order.DriverId = driverId;
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        var result = await handler.Handle(
            new GetOrderByIdQuery { OrderId = order.Id, RequestingDriverId = driverId },
            CancellationToken.None);

        Assert.Equal(order.Id, result.OrderId);
    }

    [Fact]
    public async Task Should_throw_ForbiddenException_when_requesting_driver_is_not_the_assigned_driver()
    {
        var ownerId = Guid.NewGuid();
        var assignedDriverId = Guid.NewGuid();
        var otherDriverId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        order.DriverId = assignedDriverId;
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new GetOrderByIdQuery { OrderId = order.Id, RequestingDriverId = otherDriverId },
            CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_ForbiddenException_when_requesting_driver_but_order_has_no_assigned_driver_yet()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId); // DriverId left null - not yet received
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new GetOrderByIdQuery { OrderId = order.Id, RequestingDriverId = Guid.NewGuid() },
            CancellationToken.None));
    }

    [Fact]
    public async Task Should_still_reject_a_Customer_id_that_matches_no_owner_even_when_order_has_an_assigned_driver()
    {
        // A Driver JWT must never be granted Customer-style ownership access, and a Customer
        // JWT must never be granted Driver-style assignment access - the two checks stay
        // fully independent (see GetOrderByIdQueryHandler's Day 7 comment).
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        order.DriverId = driverId;
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new GetOrderByIdQuery { OrderId = order.Id, RequestingCustomerId = otherCustomerId },
            CancellationToken.None));
    }
}
