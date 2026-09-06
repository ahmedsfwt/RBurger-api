using RBurger.Application.Orders.Queries.GetOrdersMine;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class GetOrdersMineQueryHandlerTests
{
    private static Branch SohagBranch() => new()
    {
        Id = 1,
        NameAr = "سوهاج",
        NameEn = "Sohag",
        DeliveryFee = 20m,
        IsActive = true
    };

    private static Order BuildOrder(Guid customerId, bool hasReview = false) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = 4821,
        BranchId = 1,
        Branch = SohagBranch(),
        CustomerId = customerId,
        Stage = OrderStage.Confirmed,
        CustomerName = "Ahmed Sami",
        Phone = "01012345678",
        Address = "Sohag, University street",
        Total = 255m,
        PaymentMethod = "cash",
        Review = hasReview ? new Review { Rating = 5 } : null
    };

    [Fact]
    public async Task Should_only_return_orders_belonging_to_the_requesting_customer()
    {
        var customerId = Guid.NewGuid();
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(BuildOrder(customerId));
        repository.AddedOrders.Add(BuildOrder(Guid.NewGuid())); // another customer's order
        var handler = new GetOrdersMineQueryHandler(repository);

        var result = await handler.Handle(
            new GetOrdersMineQuery { CustomerId = customerId }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Should_map_HasReview_true_when_order_has_a_review()
    {
        var customerId = Guid.NewGuid();
        var repository = new FakeOrderRepository();
        repository.AddedOrders.Add(BuildOrder(customerId, hasReview: true));
        var handler = new GetOrdersMineQueryHandler(repository);

        var result = await handler.Handle(
            new GetOrdersMineQuery { CustomerId = customerId }, CancellationToken.None);

        Assert.True(result.Items.Single().HasReview);
    }

    [Fact]
    public async Task Should_apply_documented_default_page_and_pageSize()
    {
        // §7.0: page default 1, pageSize default 20.
        var customerId = Guid.NewGuid();
        var repository = new FakeOrderRepository();
        var handler = new GetOrdersMineQueryHandler(repository);

        var result = await handler.Handle(
            new GetOrdersMineQuery { CustomerId = customerId }, CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task Should_clamp_invalid_page_and_pageSize_to_documented_defaults()
    {
        var customerId = Guid.NewGuid();
        var repository = new FakeOrderRepository();
        var handler = new GetOrdersMineQueryHandler(repository);

        var result = await handler.Handle(
            new GetOrdersMineQuery { CustomerId = customerId, Page = 0, PageSize = -5 },
            CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }
}
