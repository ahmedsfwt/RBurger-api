using RBurger.Application.Admin.Orders.Queries.GetAdminOrders;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class GetAdminOrdersQueryHandlerTests
{
    private static Order Order(int branchId, OrderStage stage, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = new Random().Next(1, 100000),
        BranchId = branchId,
        Stage = stage,
        CreatedAt = createdAt,
        CustomerName = "Ahmed Sami",
        Total = 100
    };

    private static (GetAdminOrdersQueryHandler Handler, FakeOrderRepository Orders) BuildHandler()
    {
        var orders = new FakeOrderRepository();
        var handler = new GetAdminOrdersQueryHandler(orders);
        return (handler, orders);
    }

    [Fact]
    public async Task Handle_returns_paginated_results_ordered_by_CreatedAt_descending()
    {
        var (handler, orders) = BuildHandler();
        orders.AddedOrders.Add(Order(1, OrderStage.Confirmed, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        orders.AddedOrders.Add(Order(1, OrderStage.Confirmed, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)));
        orders.AddedOrders.Add(Order(1, OrderStage.Confirmed, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)));

        var result = await handler.Handle(
            new GetAdminOrdersQuery(1, 20, null, null, null, null), default);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc), result.Items[0].CreatedAt);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Items[2].CreatedAt);
    }

    [Fact]
    public async Task Handle_filters_by_branchId()
    {
        var (handler, orders) = BuildHandler();
        orders.AddedOrders.Add(Order(1, OrderStage.Confirmed, DateTime.UtcNow));
        orders.AddedOrders.Add(Order(2, OrderStage.Confirmed, DateTime.UtcNow));

        var result = await handler.Handle(
            new GetAdminOrdersQuery(1, 20, 2, null, null, null), default);

        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.BranchId);
    }

    [Fact]
    public async Task Handle_filters_by_stage()
    {
        var (handler, orders) = BuildHandler();
        orders.AddedOrders.Add(Order(1, OrderStage.Confirmed, DateTime.UtcNow));
        orders.AddedOrders.Add(Order(1, OrderStage.Delivered, DateTime.UtcNow));

        var result = await handler.Handle(
            new GetAdminOrdersQuery(1, 20, null, OrderStage.Delivered, null, null), default);

        var item = Assert.Single(result.Items);
        Assert.Equal(OrderStage.Delivered, item.Stage);
    }

    [Fact]
    public async Task Handle_filters_by_date_range()
    {
        var (handler, orders) = BuildHandler();
        orders.AddedOrders.Add(Order(1, OrderStage.Confirmed, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        orders.AddedOrders.Add(Order(1, OrderStage.Confirmed, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)));
        orders.AddedOrders.Add(Order(1, OrderStage.Confirmed, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await handler.Handle(
            new GetAdminOrdersQuery(
                1, 20, null, null,
                new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc)),
            default);

        var item = Assert.Single(result.Items);
        Assert.Equal(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), item.CreatedAt);
    }

    [Fact]
    public async Task Handle_clamps_invalid_page_and_pageSize()
    {
        var (handler, orders) = BuildHandler();
        orders.AddedOrders.Add(Order(1, OrderStage.Confirmed, DateTime.UtcNow));

        var result = await handler.Handle(
            new GetAdminOrdersQuery(0, 0, null, null, null, null), default);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }
}
