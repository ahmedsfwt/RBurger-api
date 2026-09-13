using FluentValidation;

namespace RBurger.Application.Admin.Builder.Commands.CreateBuilderOptionGroup;

public class CreateBuilderOptionGroupCommandValidator : AbstractValidator<CreateBuilderOptionGroupCommand>
{
    public CreateBuilderOptionGroupCommandValidator()
    {
        RuleFor(x => x.GroupKey).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Options).NotEmpty();

        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.NameAr).NotEmpty().MaximumLength(60);
            option.RuleFor(o => o.NameEn).NotEmpty().MaximumLength(60);
            option.RuleFor(o => o.ExtraPrice).GreaterThanOrEqualTo(0);
        });
    }
}