using FluentValidation.TestHelper;
using RBurger.Application.Authentication.Commands.AdminLogin;
using Xunit;

namespace RBurger.Application.Tests.Authentication;

public class AdminLoginCommandValidatorTests
{
    private readonly AdminLoginCommandValidator _validator = new();

    [Fact]
    public void Should_not_have_error_when_command_is_valid()
    {
        var command = new AdminLoginCommand { Username = "admin", Password = "P@ssw0rd" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_have_error_when_Username_is_empty()
    {
        var command = new AdminLoginCommand { Username = "", Password = "P@ssw0rd" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_have_error_when_Password_is_empty()
    {
        var command = new AdminLoginCommand { Username = "admin", Password = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
