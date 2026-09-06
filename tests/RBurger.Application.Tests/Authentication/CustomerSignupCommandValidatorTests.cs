using FluentValidation.TestHelper;
using RBurger.Application.Authentication.Commands.CustomerSignup;
using Xunit;

namespace RBurger.Application.Tests.Authentication;

public class CustomerSignupCommandValidatorTests
{
    private readonly CustomerSignupCommandValidator _validator = new();

    private static CustomerSignupCommand ValidCommand() => new()
    {
        FullName = "Ahmed Sami",
        Phone = "01012345678",
        Password = "P@ssw0rd",
        Address = "Sohag, University street",
        PreferredLanguage = "ar"
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
    public void Should_have_error_when_FullName_exceeds_150_characters()
    {
        var command = ValidCommand();
        command.FullName = new string('a', 151);
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
        // Approved decision #4: no complexity rules invented - a simple non-empty password
        // must pass validation.
        var command = ValidCommand();
        command.Password = "a";
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("")]
    [InlineData("english")]
    public void Should_have_error_when_PreferredLanguage_is_not_ar_or_en(string lang)
    {
        var command = ValidCommand();
        command.PreferredLanguage = lang;
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PreferredLanguage);
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public void Should_not_have_error_when_PreferredLanguage_is_ar_or_en(string lang)
    {
        var command = ValidCommand();
        command.PreferredLanguage = lang;
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.PreferredLanguage);
    }

    [Fact]
    public void Should_not_have_error_when_Address_is_null()
    {
        var command = ValidCommand();
        command.Address = null;
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }

    [Fact]
    public void Should_have_error_when_Address_exceeds_300_characters()
    {
        var command = ValidCommand();
        command.Address = new string('a', 301);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Address);
    }
}
