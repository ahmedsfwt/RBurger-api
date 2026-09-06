using FluentValidation;

namespace RBurger.Application.Authentication.Commands.CustomerLogin;

public class CustomerLoginCommandValidator : AbstractValidator<CustomerLoginCommand>
{
    public CustomerLoginCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
