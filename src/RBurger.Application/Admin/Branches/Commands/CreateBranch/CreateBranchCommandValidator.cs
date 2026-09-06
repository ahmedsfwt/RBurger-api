using FluentValidation;

namespace RBurger.Application.Admin.Branches.Commands.CreateBranch;

public class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(100); // §6.2 Branches.NameAr/En
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DeliveryFee).GreaterThanOrEqualTo(0); // §6.2 decimal(8,2), required
        RuleFor(x => x.EtaMinMinutes).GreaterThan(0);
        RuleFor(x => x.EtaMaxMinutes).GreaterThan(0);
        // Day 14 (Backend Parity Spec §1.4) - matches BranchConfiguration's 50-char column.
        RuleFor(x => x.EstimatedDeliveryTime).MaximumLength(50);

        // §1.3: "own delivery fee and ETA range" - a range implies Min <= Max; not a literal
        // §6.2 constraint, but the minimal implication of "range" mirrors the same class of
        // decision already made elsewhere (e.g. CreateOrderCommandValidator's PaymentMethod
        // closed-set check) for a documented concept that needs one concrete rule to be usable.
        RuleFor(x => x)
            .Must(x => x.EtaMinMinutes <= x.EtaMaxMinutes)
            .WithMessage("EtaMinMinutes must be less than or equal to EtaMaxMinutes.")
            .WithName("EtaMinMinutes");
    }
}
