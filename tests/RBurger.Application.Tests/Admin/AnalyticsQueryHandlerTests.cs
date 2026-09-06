using RBurger.Application.Admin.Analytics.Queries.GetAnalyticsOverview;
using RBurger.Application.Admin.Analytics.Queries.GetDriverPerformance;
using RBurger.Application.Admin.Analytics.Queries.GetOrdersByBranch;
using RBurger.Application.Admin.Analytics.Queries.GetOrdersByStatus;
using RBurger.Application.Admin.Analytics.Queries.GetRatingDistribution;
using RBurger.Application.Admin.Analytics.Queries.GetRevenueTrend;
using RBurger.Application.Admin.Analytics.Queries.GetTopItems;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class AnalyticsQueryHandlerTests
{
    [Fact]
    public async Task GetOrdersByStatus_returns_all_four_stages_including_zero_counts()
    {
        var orders = new FakeOrderRepository();
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, Stage = OrderStage.Confirmed, CustomerName = "A", Total = 1 });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, Stage = OrderStage.Confirmed, CustomerName = "A", Total = 1 });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, Stage = OrderStage.Delivered, CustomerName = "A", Total = 1 });

        var handler = new GetOrdersByStatusQueryHandler(orders);
        var result = await handler.Handle(new GetOrdersByStatusQuery(), default);

        Assert.Equal(4, result.Count); // stages 0-3 always present
        Assert.Equal(2, result.Single(r => r.Stage == (int)OrderStage.Confirmed).Count);
        Assert.Equal(0, result.Single(r => r.Stage == (int)OrderStage.Preparing).Count);
        Assert.Equal(1, result.Single(r => r.Stage == (int)OrderStage.Delivered).Count);
    }

    [Fact]
    public async Task GetOrdersByBranch_includes_every_branch_even_with_zero_orders()
    {
        var branches = new FakeBranchRepository();
        branches.Branches.Add(new Branch { Id = 1, NameAr = "سوهاج", NameEn = "Sohag" });
        branches.Branches.Add(new Branch { Id = 2, NameAr = "جرجا", NameEn = "Girga" });
        var orders = new FakeOrderRepository();
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1 });

        var handler = new GetOrdersByBranchQueryHandler(branches, orders);
        var result = await handler.Handle(new GetOrdersByBranchQuery(), default);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result.Single(b => b.BranchId == 1).Count);
        Assert.Equal(0, result.Single(b => b.BranchId == 2).Count);
    }

    [Fact]
    public async Task GetTopItems_sums_quantities_and_orders_descending()
    {
        var orders = new FakeOrderRepository();
        var order1 = new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1 };
        order1.OrderItems.Add(new OrderItem { MenuItemId = 101, NameAr = "أ", NameEn = "Original", Quantity = 2, UnitPrice = 90 });
        order1.OrderItems.Add(new OrderItem { MenuItemId = 102, NameAr = "ب", NameEn = "Cheese", Quantity = 1, UnitPrice = 100 });
        var order2 = new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1 };
        order2.OrderItems.Add(new OrderItem { MenuItemId = 101, NameAr = "أ", NameEn = "Original", Quantity = 3, UnitPrice = 90 });
        order2.OrderItems.Add(new OrderItem { MenuItemId = null, NameAr = "مخصص", NameEn = "Custom", Quantity = 1, UnitPrice = 145 }); // custom burger, excluded
        orders.AddedOrders.Add(order1);
        orders.AddedOrders.Add(order2);

        var menuItems = new FakeMenuItemRepository();
        menuItems.MenuItems.Add(new MenuItem { Id = 101, NameAr = "أ", NameEn = "Original", CategoryId = 1, BranchId = 1 });
        menuItems.MenuItems.Add(new MenuItem { Id = 102, NameAr = "ب", NameEn = "Cheese", CategoryId = 1, BranchId = 1 });

        var handler = new GetTopItemsQueryHandler(orders, menuItems);
        var result = await handler.Handle(new GetTopItemsQuery(5), default);

        Assert.Equal(2, result.Count);
        Assert.Equal(101, result[0].MenuItemId);
        Assert.Equal(5, result[0].UnitsSold); // 2 + 3
        Assert.Equal(102, result[1].MenuItemId);
        Assert.Equal(1, result[1].UnitsSold);
    }

    [Fact]
    public async Task GetDriverPerformance_only_includes_active_drivers()
    {
        var drivers = new FakeDriverRepository();
        var activeDriver = new Driver { Id = Guid.NewGuid(), FullName = "Karim Adel", Phone = "1", PasswordHash = "x", Vehicle = "bike", BranchId = 1, IsActive = true };
        var inactiveDriver = new Driver { Id = Guid.NewGuid(), FullName = "Inactive Driver", Phone = "2", PasswordHash = "x", Vehicle = "bike", BranchId = 1, IsActive = false };
        drivers.Drivers.Add(activeDriver);
        drivers.Drivers.Add(inactiveDriver);

        var orders = new FakeOrderRepository();
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, DriverId = activeDriver.Id, Stage = OrderStage.Delivered, CustomerName = "A", Total = 1, CreatedAt = DateTime.UtcNow });

        var handler = new GetDriverPerformanceQueryHandler(drivers, orders);
        var result = await handler.Handle(new GetDriverPerformanceQuery(), default);

        var item = Assert.Single(result);
        Assert.Equal(activeDriver.Id, item.DriverId);
    }

    [Fact]
    public async Task GetRatingDistribution_returns_all_five_star_values_including_zero_counts()
    {
        var reviews = new FakeReviewRepository();
        var order = new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1 };
        reviews.AddedReviews.Add(new Review { Id = Guid.NewGuid(), OrderId = order.Id, Order = order, CustomerId = Guid.NewGuid(), Rating = 5, CreatedAt = DateTime.UtcNow });
        reviews.AddedReviews.Add(new Review { Id = Guid.NewGuid(), OrderId = order.Id, Order = order, CustomerId = Guid.NewGuid(), Rating = 5, CreatedAt = DateTime.UtcNow });

        var handler = new GetRatingDistributionQueryHandler(reviews);
        var result = await handler.Handle(new GetRatingDistributionQuery(), default);

        Assert.Equal(5, result.Count); // stars 1-5 always present
        Assert.Equal(2, result.Single(r => r.Stars == 5).Count);
        Assert.Equal(0, result.Single(r => r.Stars == 1).Count);
    }

    [Fact]
    public async Task GetRevenueTrend_sums_captured_payments_only_and_excludes_cancelled_orders()
    {
        var orders = new FakeOrderRepository();
        var today = DateTime.UtcNow.Date;

        AddOrder(orders, today, "captured", 100, isCancelled: false);
        AddOrder(orders, today, "pending", 999, isCancelled: false); // excluded: not captured
        AddOrder(orders, today, "captured", 999, isCancelled: true); // excluded: cancelled
        AddOrder(orders, today.AddDays(-1), "captured", 50, isCancelled: false);

        var handler = new GetRevenueTrendQueryHandler(orders);
        var result = await handler.Handle(new GetRevenueTrendQuery(2), default);

        Assert.Equal(2, result.Count);
        Assert.Equal(today.AddDays(-1).ToString("yyyy-MM-dd"), result[0].Date);
        Assert.Equal(50m, result[0].Revenue);
        Assert.Equal(today.ToString("yyyy-MM-dd"), result[1].Date);
        Assert.Equal(100m, result[1].Revenue); // only the captured, non-cancelled order counted
    }

    [Fact]
    public async Task GetRevenueTrend_returns_zero_for_days_with_no_captured_revenue()
    {
        var orders = new FakeOrderRepository();
        var handler = new GetRevenueTrendQueryHandler(orders);

        var result = await handler.Handle(new GetRevenueTrendQuery(3), default);

        Assert.Equal(3, result.Count);
        Assert.All(result, point => Assert.Equal(0m, point.Revenue));
    }

    private static void AddOrder(
        FakeOrderRepository orders, DateTime date, string paymentStatus, decimal amount, bool isCancelled)
    {
        var orderId = Guid.NewGuid();
        orders.AddedOrders.Add(new Order
        {
            Id = orderId,
            BranchId = 1,
            CustomerName = "A",
            Total = amount,
            CreatedAt = date,
            IsCancelled = isCancelled,
            Payment = new Payment { Id = Guid.NewGuid(), OrderId = orderId, Status = paymentStatus, Amount = amount }
        });
    }

    [Fact]
    public async Task GetOrdersByStatus_excludes_cancelled_orders()
    {
        var orders = new FakeOrderRepository();
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, Stage = OrderStage.Confirmed, CustomerName = "A", Total = 1 });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, Stage = OrderStage.Confirmed, CustomerName = "A", Total = 1, IsCancelled = true });

        var handler = new GetOrdersByStatusQueryHandler(orders);
        var result = await handler.Handle(new GetOrdersByStatusQuery(), default);

        Assert.Equal(1, result.Single(r => r.Stage == (int)OrderStage.Confirmed).Count);
    }

    [Fact]
    public async Task GetOrdersByBranch_excludes_cancelled_orders()
    {
        var branches = new FakeBranchRepository();
        branches.Branches.Add(new Branch { Id = 1, NameAr = "سوهاج", NameEn = "Sohag" });
        var orders = new FakeOrderRepository();
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1 });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1, IsCancelled = true });

        var handler = new GetOrdersByBranchQueryHandler(branches, orders);
        var result = await handler.Handle(new GetOrdersByBranchQuery(), default);

        Assert.Equal(1, result.Single(b => b.BranchId == 1).Count);
    }

    [Fact]
    public async Task GetTopItems_excludes_items_from_cancelled_orders()
    {
        var orders = new FakeOrderRepository();
        var live = new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1 };
        live.OrderItems.Add(new OrderItem { MenuItemId = 101, NameAr = "أ", NameEn = "Original", Quantity = 2, UnitPrice = 90 });
        var cancelled = new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1, IsCancelled = true };
        cancelled.OrderItems.Add(new OrderItem { MenuItemId = 101, NameAr = "أ", NameEn = "Original", Quantity = 99, UnitPrice = 90 });
        orders.AddedOrders.Add(live);
        orders.AddedOrders.Add(cancelled);

        var menuItems = new FakeMenuItemRepository();
        menuItems.MenuItems.Add(new MenuItem { Id = 101, NameAr = "أ", NameEn = "Original", CategoryId = 1, BranchId = 1 });

        var handler = new GetTopItemsQueryHandler(orders, menuItems);
        var result = await handler.Handle(new GetTopItemsQuery(5), default);

        Assert.Equal(2, Assert.Single(result).UnitsSold); // only the live order's 2 units counted
    }

    [Fact]
    public async Task GetDriverPerformance_excludes_cancelled_deliveries()
    {
        var drivers = new FakeDriverRepository();
        var driver = new Driver { Id = Guid.NewGuid(), FullName = "Karim Adel", Phone = "1", PasswordHash = "x", Vehicle = "bike", BranchId = 1, IsActive = true };
        drivers.Drivers.Add(driver);

        var orders = new FakeOrderRepository();
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, DriverId = driver.Id, Stage = OrderStage.Delivered, CustomerName = "A", Total = 1 });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, DriverId = driver.Id, Stage = OrderStage.Delivered, CustomerName = "A", Total = 1, IsCancelled = true });

        var handler = new GetDriverPerformanceQueryHandler(drivers, orders);
        var result = await handler.Handle(new GetDriverPerformanceQuery(), default);

        Assert.Equal(1, Assert.Single(result).DeliveriesCompleted);
    }

    [Fact]
    public async Task GetRatingDistribution_excludes_reviews_for_cancelled_orders()
    {
        var reviews = new FakeReviewRepository();
        var liveOrder = new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1 };
        var cancelledOrder = new Order { Id = Guid.NewGuid(), BranchId = 1, CustomerName = "A", Total = 1, IsCancelled = true };
        reviews.AddedReviews.Add(new Review { Id = Guid.NewGuid(), OrderId = liveOrder.Id, Order = liveOrder, CustomerId = Guid.NewGuid(), Rating = 5, CreatedAt = DateTime.UtcNow });
        reviews.AddedReviews.Add(new Review { Id = Guid.NewGuid(), OrderId = cancelledOrder.Id, Order = cancelledOrder, CustomerId = Guid.NewGuid(), Rating = 5, CreatedAt = DateTime.UtcNow });

        var handler = new GetRatingDistributionQueryHandler(reviews);
        var result = await handler.Handle(new GetRatingDistributionQuery(), default);

        Assert.Equal(1, result.Single(r => r.Stars == 5).Count);
    }

    [Fact]
    public async Task GetAnalyticsOverview_computes_captured_revenue_and_isolates_completionRate_as_null()
    {
        var orders = new FakeOrderRepository();
        var today = DateTime.UtcNow.Date;
        AddOrder(orders, today, "captured", 100, isCancelled: false);
        AddOrder(orders, today, "pending", 999, isCancelled: false); // excluded: not captured
        AddOrder(orders, today, "captured", 999, isCancelled: true); // excluded: cancelled

        var handler = new GetAnalyticsOverviewQueryHandler(orders);
        var result = await handler.Handle(new GetAnalyticsOverviewQuery("today"), default);

        Assert.Equal(100m, result.TotalRevenue);
        Assert.Equal(1, result.TotalOrders);
        Assert.Equal(100m, result.AvgOrderValue);

        // Backend Parity Spec §11: isolated, not fabricated - the field is present (null), the
        // endpoint is not a 501.
        Assert.Null(result.CompletionRatePercent);
        Assert.Null(result.Deltas.CompletionRate);
    }

    [Fact]
    public async Task GetAnalyticsOverview_weekly_delta_compares_current_week_to_previous_week()
    {
        var orders = new FakeOrderRepository();
        var today = DateTime.UtcNow.Date;
        AddOrder(orders, today, "captured", 200, isCancelled: false); // current week
        AddOrder(orders, today.AddDays(-10), "captured", 100, isCancelled: false); // previous week

        var handler = new GetAnalyticsOverviewQueryHandler(orders);
        var result = await handler.Handle(new GetAnalyticsOverviewQuery("7d"), default);

        Assert.Equal(100m, result.Deltas.Revenue); // (200-100)/100 * 100 = 100% growth
    }
}
