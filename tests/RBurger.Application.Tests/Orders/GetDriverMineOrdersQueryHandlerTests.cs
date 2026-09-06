using FluentValidation.TestHelper;
using RBurger.Application.Orders.Queries.GetDriverMineOrders;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class GetDriverMineOrdersQueryHandlerTests
{
    private static Order BuildOrder(Guid? driverId, OrderStage stage) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = 4821,
        BranchId = 1,
        CustomerId = Guid.NewGuid(),
        DriverId = driverId,
        Stage = stage,
        CustomerName = "Ahmed Sami",
        Phone = "01012345678",
        Address = "Sohag, University street",
        Subtotal = 235m,
        DeliveryFee = 20m,
        Total = 255m,
        PaymentMethod = "cash"
    };

    [Fact]
    public async Task Active_status_returns_orders_assigned_to_driver_that_are_not_yet_delivered()
    {
        var driverId = Guid.NewGuid();
        var orders = new FakeOrderRepository();
        var preparing = BuildOrder(driverId, OrderStage.Preparing);
        var onTheWay = BuildOrder(driverId, OrderStage.OnTheWay);
        var delivered = BuildOrder(driverId, OrderStage.Delivered);
        var otherDriver = BuildOrder(Guid.NewGuid(), OrderStage.Preparing);
        orders.AddedOrders.AddRange(new[] { preparing, onTheWay, delivered, otherDriver });

        var handler = new GetDriverMineOrdersQueryHandler(orders);

        var result = await handler.Handle(
            new GetDriverMineOrdersQuery { DriverId = driverId, Status = "active" }, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.OrderId == preparing.Id);
        Assert.Contains(result, r => r.OrderId == onTheWay.Id);
        Assert.DoesNotContain(result, r => r.OrderId == delivered.Id);
        Assert.DoesNotContain(result, r => r.OrderId == otherDriver.Id);
    }

    [Fact]
    public async Task Completed_status_returns_only_orders_delivered_today_by_this_driver()
    {
        var driverId = Guid.NewGuid();
        var orders = new FakeOrderRepository();
        var deliveredToday = BuildOrder(driverId, OrderStage.Delivered);
        var deliveredYesterday = BuildOrder(driverId, OrderStage.Delivered);
        var otherDriverDeliveredToday = BuildOrder(Guid.NewGuid(), OrderStage.Delivered);
        orders.AddedOrders.AddRange(new[] { deliveredToday, deliveredYesterday, otherDriverDeliveredToday });

        var today = DateTime.UtcNow.Date;
        orders.AddedStatusEvents.Add(new OrderStatusEvent
        {
            OrderId = deliveredToday.Id, Stage = OrderStage.Delivered, TriggeredBy = "driver",
            ActorId = driverId, Timestamp = today.AddHours(10)
        });
        orders.AddedStatusEvents.Add(new OrderStatusEvent
        {
            OrderId = deliveredYesterday.Id, Stage = OrderStage.Delivered, TriggeredBy = "driver",
            ActorId = driverId, Timestamp = today.AddDays(-1).AddHours(10)
        });
        orders.AddedStatusEvents.Add(new OrderStatusEvent
        {
            OrderId = otherDriverDeliveredToday.Id, Stage = OrderStage.Delivered, TriggeredBy = "driver",
            ActorId = Guid.NewGuid(), Timestamp = today.AddHours(11)
        });

        var handler = new GetDriverMineOrdersQueryHandler(orders);

        var result = await handler.Handle(
            new GetDriverMineOrdersQuery { DriverId = driverId, Status = "completed" }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(deliveredToday.Id, result[0].OrderId);
    }
}

public class GetDriverMineOrdersQueryValidatorTests
{
    private readonly GetDriverMineOrdersQueryValidator _validator = new();

    [Theory]
    [InlineData("active")]
    [InlineData("completed")]
    public void Should_not_have_error_for_documented_status_values(string status)
    {
        var result = _validator.TestValidate(new GetDriverMineOrdersQuery { DriverId = Guid.NewGuid(), Status = status });
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pending")]
    [InlineData("ACTIVE")]
    public void Should_have_error_for_undocumented_status_values(string status)
    {
        var result = _validator.TestValidate(new GetDriverMineOrdersQuery { DriverId = Guid.NewGuid(), Status = status });
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
