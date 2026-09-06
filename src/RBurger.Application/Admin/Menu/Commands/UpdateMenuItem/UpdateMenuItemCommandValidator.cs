using FluentValidation;

namespace RBurger.Application.Admin.Menu.Commands.UpdateMenuItem;

public class UpdateMenuItemCommandValidator : AbstractValidator<UpdateMenuItemCommand>
{
    public UpdateMenuItemCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150).When(x => x.NameAr is not null);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(150).When(x => x.NameEn is not null);
        RuleFor(x => x.DescriptionAr).NotEmpty().MaximumLength(400).When(x => x.DescriptionAr is not null);
        RuleFor(x => x.DescriptionEn).NotEmpty().MaximumLength(400).When(x => x.DescriptionEn is not null);
        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price is not null);
        RuleFor(x => x.CategoryKey).NotEmpty().MaximumLength(30).When(x => x.CategoryKey is not null);
    }
}
