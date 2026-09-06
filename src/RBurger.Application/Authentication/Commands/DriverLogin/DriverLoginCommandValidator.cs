using FluentValidation;

namespace RBurger.Application.Authentication.Commands.DriverLogin;

public class DriverLoginCommandValidator : AbstractValidator<DriverLoginCommand>
{
    public DriverLoginCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
