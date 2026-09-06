using FluentValidation.TestHelper;
using RBurger.Application.Admin.Drivers.Commands.CreateDriver;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class CreateDriverCommandValidatorTests
{
    private readonly CreateDriverCommandValidator _validator = new();

    private static CreateDriverCommand ValidCommand() => new()
    {
        FullName = "Karim Adel",
        Phone = "01099988877",
        Password = "P@ssw0rd",
        Vehicle = "bike",
        BranchId = 1
    };

    [Fact]
    public void Should_not_have_error_when_command_is_valid()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_have_error_when_FullName_is_empty()
    {
        var command = ValidCommand();
        command.FullName = "";
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Should_have_error_when_Phone_is_empty()
    {
        var command = ValidCommand();
        command.Phone = "";
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void Should_have_error_when_Password_is_empty()
    {
        var command = ValidCommand();
        command.Password = "";
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_not_have_error_when_Password_has_no_complexity()
    {
        // Approved decision #4 (carried forward from Day 3): no invented complexity rules.
        var command = ValidCommand();
        command.Password = "a";
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("bike")]
    [InlineData("bicycle")]
    [InlineData("car")]
    public void Should_not_have_error_for_documented_vehicle_values(string vehicle)
    {
        var command = ValidCommand();
        command.Vehicle = vehicle;
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Vehicle);
    }

    [Theory]
    [InlineData("scooter")]
    [InlineData("")]
    [InlineData("Bike")]
    public void Should_have_error_for_undocumented_vehicle_values(string vehicle)
    {
        var command = ValidCommand();
        command.Vehicle = vehicle;
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Vehicle);
    }

    [Fact]
    public void Should_have_error_when_BranchId_is_zero_or_negative()
    {
        var command = ValidCommand();
        command.BranchId = 0;
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
    }
}
