using FluentValidation;

namespace RBurger.Application.Admin.Menu.Commands.UploadMenuItemImage;

public class UploadMenuItemImageCommandValidator : AbstractValidator<UploadMenuItemImageCommand>
{
    // §7.6.1: "validates content-type (image/jpeg, image/png, image/webp) and a 5 MB max size".
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public UploadMenuItemImageCommandValidator()
    {
        RuleFor(x => x.MenuItemId).GreaterThan(0);

        RuleFor(x => x.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Image content-type must be image/jpeg, image/png, or image/webp.");

        RuleFor(x => x.ContentLength)
            .GreaterThan(0)
            .WithMessage("The uploaded file is empty.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("The uploaded file exceeds the 5 MB maximum size.");

        // §5.5/§7.6.1: "Idempotency-Key header required on ... POST /admin/menu-items/{id}/image".
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("The Idempotency-Key header is required.");
    }
}
