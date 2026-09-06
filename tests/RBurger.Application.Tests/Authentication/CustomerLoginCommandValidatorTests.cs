using FluentValidation.TestHelper;
using RBurger.Application.Authentication.Commands.CustomerLogin;
using Xunit;

namespace RBurger.Application.Tests.Authentication;

public class CustomerLoginCommandValidatorTests
{
    private readonly CustomerLoginCommandValidator _validator = new();

    [Fact]
    public void Should_not_have_error_when_command_is_valid()
    {
        var command = new CustomerLoginCommand { Phone = "01012345678", Password = "P@ssw0rd" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_have_error_when_Phone_is_empty()
    {
        var command = new CustomerLoginCommand { Phone = "", Password = "P@ssw0rd" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void Should_have_error_when_Password_is_empty()
    {
        var command = new CustomerLoginCommand { Phone = "01012345678", Password = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
