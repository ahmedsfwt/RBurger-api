using FluentValidation;

namespace RBurger.Application.Admin.Drivers.Commands.CreateDriver;

public class CreateDriverCommandValidator : AbstractValidator<CreateDriverCommand>
{
    public CreateDriverCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150); // §6.2 Drivers.FullName
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20); // §6.2 Drivers.Phone

        // Approved decision #4 (Day 3, carried forward): no invented complexity rules - a
        // simple non-empty password must pass validation, mirroring
        // CustomerSignupCommandValidator/DriverLoginCommandValidator's Password rule exactly.
        RuleFor(x => x.Password).NotEmpty();

        // §6.2 Drivers.Vehicle nvarchar(20), documented closed set "bike | bicycle | car",
        // mirroring CreateOrderCommandValidator's PaymentMethod closed-set check style.
        RuleFor(x => x.Vehicle)
            .NotEmpty()
            .Must(v => v is "bike" or "bicycle" or "car")
            .WithMessage("Vehicle must be 'bike', 'bicycle' or 'car'.");

        RuleFor(x => x.BranchId).GreaterThan(0);
    }
}
