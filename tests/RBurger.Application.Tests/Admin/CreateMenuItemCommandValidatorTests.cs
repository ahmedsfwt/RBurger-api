using FluentValidation.TestHelper;
using RBurger.Application.Admin.Menu.Commands.CreateMenuItem;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class CreateMenuItemCommandValidatorTests
{
    private readonly CreateMenuItemCommandValidator _validator = new();

    private static CreateMenuItemCommand ValidCommand() => new()
    {
        CategoryKey = "burgers",
        NameAr = "أورجينال",
        NameEn = "Original",
        DescriptionAr = "وصف",
        DescriptionEn = "description",
        Price = 90,
        BranchId = 1
    };

    [Fact]
    public void Should_not_have_error_when_command_is_valid()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_have_error_when_CategoryKey_is_empty()
    {
        var command = ValidCommand();
        command.CategoryKey = "";
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.CategoryKey);
    }

    [Fact]
    public void Should_have_error_when_Price_is_zero_or_negative()
    {
        var command = ValidCommand();
        command.Price = 0;
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Should_have_error_when_BranchId_is_zero()
    {
        var command = ValidCommand();
        command.BranchId = 0;
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.BranchId);
    }

    [Fact]
    public void Should_have_error_when_NameAr_is_empty()
    {
        var command = ValidCommand();
        command.NameAr = "";
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.NameAr);
    }
}
