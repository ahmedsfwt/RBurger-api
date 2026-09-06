using FluentValidation.TestHelper;
using RBurger.Application.Orders.Commands.CreateOrder;
using RBurger.Application.Orders.DTOs;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    private static CreateOrderCommand ValidCommand() => new()
    {
        BranchId = 1,
        Items = new List<CreateOrderItemDto>
        {
            new() { MenuItemId = 101, Quantity = 2 }
        },
        CustomerName = "Ahmed Sami",
        CustomerPhone = "01012345678",
        DeliveryAddress = "Sohag, University street",
        Notes = "No pickles please",
        PaymentMethod = "cash",
        IdempotencyKey = "a-unique-key"
    };

    [Fact]
    public void Should_not_have_error_when_command_is_valid()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_have_error_when_BranchId_is_zero()
    {
        var command = ValidCommand();
        command.BranchId = 0;
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void Should_have_error_when_Items_is_empty()
    {
        var command = ValidCommand();
        command.Items = new List<CreateOrderItemDto>();
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Should_have_error_when_item_Quantity_is_zero()
    {
        var command = ValidCommand();
        command.Items[0].Quantity = 0;
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Theory]
    [InlineData("")]
    [InlineData("bank_transfer")]
    public void Should_have_error_when_PaymentMethod_is_not_cash_or_card(string method)
    {
        var command = ValidCommand();
        command.PaymentMethod = method;
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Theory]
    [InlineData("cash")]
    [InlineData("card")]
    public void Should_not_have_error_when_PaymentMethod_is_cash_or_card(string method)
    {
        var command = ValidCommand();
        command.PaymentMethod = method;
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Fact]
    public void Should_have_error_when_IdempotencyKey_is_missing()
    {
        // Approved decision #1: Idempotency-Key header is required on POST /orders (§5.5, §7.4).
        var command = ValidCommand();
        command.IdempotencyKey = null;
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void Should_not_have_error_when_Notes_is_null()
    {
        var command = ValidCommand();
        command.Notes = null;
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_have_error_when_custom_item_has_no_CustomName()
    {
        var command = ValidCommand();
        command.Items = new List<CreateOrderItemDto>
        {
            new()
            {
                MenuItemId = null,
                Quantity = 1,
                CustomName = null,
                UnitPrice = 145
            }
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("Items[0].CustomName");
    }

    [Fact]
    public void Should_have_error_when_custom_item_has_no_UnitPrice()
    {
        var command = ValidCommand();
        command.Items = new List<CreateOrderItemDto>
        {
            new()
            {
                MenuItemId = null,
                Quantity = 1,
                CustomName = new LocalizedTextDto { Ar = "برجرك المميز", En = "Your Signature Burger" },
                UnitPrice = null
            }
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice");
    }

    [Fact]
    public void Should_not_have_error_when_custom_item_has_CustomName_and_UnitPrice()
    {
        var command = ValidCommand();
        command.Items = new List<CreateOrderItemDto>
        {
            new()
            {
                MenuItemId = null,
                Quantity = 1,
                CustomName = new LocalizedTextDto { Ar = "برجرك المميز", En = "Your Signature Burger" },
                CustomDescription = new LocalizedTextDto { Ar = "...", En = "..." },
                UnitPrice = 145
            }
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_not_have_error_when_catalog_item_omits_CustomName_and_UnitPrice()
    {
        var command = ValidCommand();
        command.Items = new List<CreateOrderItemDto>
        {
            new() { MenuItemId = 101, Quantity = 2 }
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
