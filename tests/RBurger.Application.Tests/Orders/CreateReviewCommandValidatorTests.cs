using FluentValidation.TestHelper;
using RBurger.Application.Orders.Commands.CreateReview;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class CreateReviewCommandValidatorTests
{
    private readonly CreateReviewCommandValidator _validator = new();

    private static CreateReviewCommand ValidCommand() => new()
    {
        OrderId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Rating = 5,
        Comment = "الاكل كان سخن ووصل بسرعة"
    };

    [Fact]
    public void Should_not_have_error_when_command_is_valid()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_not_have_error_when_rating_is_1()
    {
        var command = ValidCommand();
        command.Rating = 1;
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Should_not_have_error_when_rating_is_5()
    {
        var command = ValidCommand();
        command.Rating = 5;
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Should_have_error_when_rating_is_0()
    {
        var command = ValidCommand();
        command.Rating = 0;
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Should_have_error_when_rating_is_6()
    {
        var command = ValidCommand();
        command.Rating = 6;
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Should_not_have_error_when_comment_is_omitted()
    {
        var command = ValidCommand();
        command.Comment = null;
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void Should_not_have_error_when_comment_is_exactly_500_characters()
    {
        var command = ValidCommand();
        command.Comment = new string('a', 500);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void Should_have_error_when_comment_is_501_characters()
    {
        var command = ValidCommand();
        command.Comment = new string('a', 501);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }
}
