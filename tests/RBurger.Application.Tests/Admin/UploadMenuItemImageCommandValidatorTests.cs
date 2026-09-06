using FluentValidation.TestHelper;
using RBurger.Application.Admin.Menu.Commands.UploadMenuItemImage;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class UploadMenuItemImageCommandValidatorTests
{
    private readonly UploadMenuItemImageCommandValidator _validator = new();

    private static UploadMenuItemImageCommand ValidCommand() => new()
    {
        MenuItemId = 106,
        Content = new MemoryStream(new byte[100]),
        ContentType = "image/jpeg",
        ContentLength = 100,
        IdempotencyKey = "abc-123"
    };

    [Fact]
    public void Should_not_have_error_when_command_is_valid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("")]
    public void Should_have_error_when_ContentType_is_not_allowed(string contentType)
    {
        var command = ValidCommand();
        command.ContentType = contentType;
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void Should_not_have_error_for_each_allowed_ContentType(string contentType)
    {
        var command = ValidCommand();
        command.ContentType = contentType;
        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void Should_have_error_when_file_exceeds_5MB()
    {
        var command = ValidCommand();
        command.ContentLength = 5 * 1024 * 1024 + 1;
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ContentLength);
    }

    [Fact]
    public void Should_have_error_when_file_is_empty()
    {
        var command = ValidCommand();
        command.ContentLength = 0;
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ContentLength);
    }

    [Fact]
    public void Should_have_error_when_IdempotencyKey_is_missing()
    {
        var command = ValidCommand();
        command.IdempotencyKey = null;
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }
}
