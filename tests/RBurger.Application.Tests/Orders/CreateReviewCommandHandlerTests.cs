using RBurger.Application.Common.Exceptions;
using RBurger.Application.Orders.Commands.CreateReview;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class CreateReviewCommandHandlerTests
{
    private static Order BuildReceivedOrder(Guid ownerCustomerId)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = 4821,
            BranchId = 1,
            CustomerId = ownerCustomerId,
            Stage = OrderStage.Delivered,
            CustomerName = "Ahmed Sami",
            Phone = "01012345678",
            Address = "Sohag, University street",
            Subtotal = 235m,
            DeliveryFee = 20m,
            Total = 255m,
            PaymentMethod = "cash",
            CustomerReceivedAt = DateTime.UtcNow.AddMinutes(-2)
        };
    }

    private static (CreateReviewCommandHandler Handler, FakeOrderRepository Orders, FakeReviewRepository Reviews)
        BuildHandler()
    {
        var orders = new FakeOrderRepository();
        var reviews = new FakeReviewRepository();
        var handler = new CreateReviewCommandHandler(orders, reviews);
        return (handler, orders, reviews);
    }

    [Fact]
    public async Task Should_create_review_when_order_is_owned_and_received()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildReceivedOrder(ownerId);
        var (handler, orders, reviews) = BuildHandler();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(
            new CreateReviewCommand { OrderId = order.Id, CustomerId = ownerId, Rating = 5, Comment = "Great!" },
            CancellationToken.None);

        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(5, result.Rating);
        Assert.Equal("Great!", result.Comment);
        Assert.Single(reviews.AddedReviews);
        // correct OrderId is persisted (test case 14).
        Assert.Equal(order.Id, reviews.AddedReviews[0].OrderId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task Should_accept_boundary_rating_values(int rating)
    {
        var ownerId = Guid.NewGuid();
        var order = BuildReceivedOrder(ownerId);
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(
            new CreateReviewCommand { OrderId = order.Id, CustomerId = ownerId, Rating = rating },
            CancellationToken.None);

        Assert.Equal(rating, result.Rating);
    }

    // Rating range (0/6 rejected) is a FluentValidation concern - exercised in
    // CreateReviewCommandValidatorTests, mirroring CreateOrderCommandValidatorTests'
    // handler/validator split.

    [Fact]
    public async Task Should_accept_review_with_comment_omitted()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildReceivedOrder(ownerId);
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);

        var result = await handler.Handle(
            new CreateReviewCommand { OrderId = order.Id, CustomerId = ownerId, Rating = 4, Comment = null },
            CancellationToken.None);

        Assert.Null(result.Comment);
    }

    [Fact]
    public async Task Should_accept_comment_of_exactly_500_characters()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildReceivedOrder(ownerId);
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);
        var comment = new string('a', 500);

        var result = await handler.Handle(
            new CreateReviewCommand { OrderId = order.Id, CustomerId = ownerId, Rating = 3, Comment = comment },
            CancellationToken.None);

        Assert.Equal(500, result.Comment!.Length);
    }

    // Comment length 501 rejected is a FluentValidation concern - exercised in
    // CreateReviewCommandValidatorTests.

    [Fact]
    public async Task Should_throw_NotFoundException_when_order_does_not_exist()
    {
        var (handler, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new CreateReviewCommand { OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), Rating = 5 },
            CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_ForbiddenException_when_requesting_customer_is_not_the_owner()
    {
        var ownerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var order = BuildReceivedOrder(ownerId);
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new CreateReviewCommand { OrderId = order.Id, CustomerId = otherCustomerId, Rating = 5 },
            CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_UnprocessableEntityException_when_CustomerReceivedAt_is_null()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildReceivedOrder(ownerId);
        order.CustomerReceivedAt = null;
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);

        await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(
            new CreateReviewCommand { OrderId = order.Id, CustomerId = ownerId, Rating = 5 },
            CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_ConflictException_when_a_review_already_exists_for_the_order()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildReceivedOrder(ownerId);
        var (handler, orders, reviews) = BuildHandler();
        orders.AddedOrders.Add(order);
        reviews.AddedReviews.Add(new Review
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            CustomerId = ownerId,
            Rating = 4,
            CreatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CreateReviewCommand { OrderId = order.Id, CustomerId = ownerId, Rating = 5 },
            CancellationToken.None));

        // No second review was added alongside the pre-existing one.
        Assert.Single(reviews.AddedReviews);
    }

    [Fact]
    public async Task Should_persist_CreatedAt_as_UTC()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildReceivedOrder(ownerId);
        var (handler, orders, _) = BuildHandler();
        orders.AddedOrders.Add(order);
        var before = DateTime.UtcNow;

        var result = await handler.Handle(
            new CreateReviewCommand { OrderId = order.Id, CustomerId = ownerId, Rating = 5 },
            CancellationToken.None);

        var after = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Utc, result.CreatedAt.Kind);
        Assert.InRange(result.CreatedAt, before, after);
    }
}
