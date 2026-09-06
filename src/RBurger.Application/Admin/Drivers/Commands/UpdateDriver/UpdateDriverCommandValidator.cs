using FluentValidation;

namespace RBurger.Application.Admin.Drivers.Commands.UpdateDriver;

public class UpdateDriverCommandValidator : AbstractValidator<UpdateDriverCommand>
{
    public UpdateDriverCommandValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(150).When(x => x.FullName is not null);

        RuleFor(x => x.Vehicle)
            .Must(v => v is "bike" or "bicycle" or "car")
            .WithMessage("Vehicle must be 'bike', 'bicycle' or 'car'.")
            .When(x => x.Vehicle is not null);

        RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId is not null);

        // Mirrors CreateDriverCommandValidator: no invented complexity rules, only non-empty
        // when a password change is actually requested.
        RuleFor(x => x.Password).NotEmpty().When(x => x.Password is not null);
    }
}
