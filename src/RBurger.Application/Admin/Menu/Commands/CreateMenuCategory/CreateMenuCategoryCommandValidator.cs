using FluentValidation;

namespace RBurger.Application.Admin.Menu.Commands.CreateMenuCategory;

public class CreateMenuCategoryCommandValidator : AbstractValidator<CreateMenuCategoryCommand>
{
    public CreateMenuCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryKey).NotEmpty().MaximumLength(30);
        RuleFor(x => x.LabelAr).NotEmpty().MaximumLength(60);
        RuleFor(x => x.LabelEn).NotEmpty().MaximumLength(60);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}