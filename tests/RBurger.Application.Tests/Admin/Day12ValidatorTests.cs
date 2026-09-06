using RBurger.Application.Admin.Orders.Queries.GetAdminOrders;
using RBurger.Application.Payments.Commands.ChargePayment;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class Day12ValidatorTests
{
    [Fact]
    public void GetAdminOrdersQueryValidator_rejects_out_of_range_stage()
    {
        var validator = new GetAdminOrdersQueryValidator();

        var result = validator.Validate(new GetAdminOrdersQuery(1, 20, null, (OrderStage)99, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetAdminOrdersQueryValidator_accepts_valid_stage()
    {
        var validator = new GetAdminOrdersQueryValidator();

        var result = validator.Validate(new GetAdminOrdersQuery(1, 20, null, OrderStage.Delivered, null, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetAdminOrdersQueryValidator_rejects_dateFrom_after_dateTo()
    {
        var validator = new GetAdminOrdersQueryValidator();

        var result = validator.Validate(new GetAdminOrdersQuery(
            1, 20, null, null,
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChargePaymentCommandValidator_rejects_missing_idempotency_key()
    {
        var validator = new ChargePaymentCommandValidator();

        var result = validator.Validate(new ChargePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChargePaymentCommandValidator_accepts_present_idempotency_key()
    {
        var validator = new ChargePaymentCommandValidator();

        var result = validator.Validate(new ChargePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), "key-1"));

        Assert.True(result.IsValid);
    }
}
