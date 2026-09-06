using FluentValidation;

namespace RBurger.Application.Admin.Menu.Commands.CreateMenuItem;

// Simple DTO/shape validation only (mirrors CreateOrderCommandValidator's separation of
// concerns). Business rules that need a database read (categoryKey/branchId existence) live
// in CreateMenuItemCommandHandler instead.
public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
    {
        RuleFor(x => x.CategoryKey).NotEmpty().MaximumLength(30); // §6.2 MenuCategories.Key
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150); // §6.2 MenuItems.NameAr/En
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DescriptionAr).NotEmpty().MaximumLength(400); // §6.2 MenuItems.DescriptionAr/En
        RuleFor(x => x.DescriptionEn).NotEmpty().MaximumLength(400);
        RuleFor(x => x.Price).GreaterThan(0); // §6.2 decimal(8,2), required
        RuleFor(x => x.BranchId).GreaterThan(0); // Day 10 approved addition - see command comment
    }
}
