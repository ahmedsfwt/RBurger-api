using FluentValidation;

namespace RBurger.Application.Admin.Branches.Commands.UpdateBranch;

public class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.DeliveryFee).GreaterThanOrEqualTo(0).When(x => x.DeliveryFee is not null);
        RuleFor(x => x.EtaMinMinutes).GreaterThan(0).When(x => x.EtaMinMinutes is not null);
        RuleFor(x => x.EtaMaxMinutes).GreaterThan(0).When(x => x.EtaMaxMinutes is not null);
        RuleFor(x => x.EstimatedDeliveryTime).MaximumLength(50);
    }
}
