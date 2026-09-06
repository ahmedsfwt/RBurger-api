using RBurger.Application.Admin.Reviews.Commands.DeleteAdminReview;
using RBurger.Application.Admin.Reviews.Queries.GetAdminReviews;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class AdminReviewsHandlerTests
{
    private static (Order Order, Review Review) OrderWithReview(int orderNumber, string customerName, byte rating, DateTime createdAt)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            BranchId = 1,
            CustomerName = customerName,
            Total = 100
        };
        var review = new Review
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            CustomerId = Guid.NewGuid(),
            Rating = rating,
            Comment = "Great!",
            CreatedAt = createdAt
        };
        return (order, review);
    }

    [Fact]
    public async Task GetAdminReviews_returns_paginated_results_with_orderNumber_and_customerName()
    {
        var reviews = new FakeReviewRepository();
        var (_, review) = OrderWithReview(4821, "Ahmed Sami", 5, DateTime.UtcNow);
        reviews.AddedReviews.Add(review);
        var handler = new GetAdminReviewsQueryHandler(reviews);

        var result = await handler.Handle(new GetAdminReviewsQuery(1, 20), default);

        var item = Assert.Single(result.Items);
        Assert.Equal(4821, item.OrderNumber);
        Assert.Equal("Ahmed Sami", item.CustomerName);
        Assert.Equal((byte)5, item.Rating);
    }

    [Fact]
    public async Task GetAdminReviews_orders_by_CreatedAt_descending()
    {
        var reviews = new FakeReviewRepository();
        var (_, older) = OrderWithReview(1, "A", 3, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var (_, newer) = OrderWithReview(2, "B", 4, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        reviews.AddedReviews.Add(older);
        reviews.AddedReviews.Add(newer);
        var handler = new GetAdminReviewsQueryHandler(reviews);

        var result = await handler.Handle(new GetAdminReviewsQuery(1, 20), default);

        Assert.Equal(2, result.Items[0].OrderNumber);
        Assert.Equal(1, result.Items[1].OrderNumber);
    }

    [Fact]
    public async Task DeleteAdminReview_throws_NotFoundException_when_review_does_not_exist()
    {
        var reviews = new FakeReviewRepository();
        var handler = new DeleteAdminReviewCommandHandler(reviews);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteAdminReviewCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task DeleteAdminReview_deletes_existing_review()
    {
        var reviews = new FakeReviewRepository();
        var (_, review) = OrderWithReview(1, "A", 5, DateTime.UtcNow);
        reviews.AddedReviews.Add(review);
        var handler = new DeleteAdminReviewCommandHandler(reviews);

        await handler.Handle(new DeleteAdminReviewCommand(review.Id), default);

        Assert.Empty(reviews.AddedReviews);
    }
}
