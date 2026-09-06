using FluentValidation;

namespace RBurger.Application.Authentication.Commands.CustomerSignup;

public class CustomerSignupCommandValidator : AbstractValidator<CustomerSignupCommand>
{
    public CustomerSignupCommandValidator()
    {
        // §6.2: Customers.FullName nvarchar(150), required.
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150);

        // §6.2: Customers.Phone nvarchar(20), unique, required.
        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(20);

        // §6.2: Customers.PasswordHash required. Approved decision #4: no complexity rules
        // (no minimum length, casing, digit, or special-character requirements) beyond presence.
        RuleFor(x => x.Password)
            .NotEmpty();

        // §6.2: Customers.DefaultAddress nvarchar(300), nullable.
        RuleFor(x => x.Address)
            .MaximumLength(300)
            .When(x => x.Address is not null);

        // §6.2: Customers.PreferredLanguage nvarchar(5), "ar | en".
        // §2.5: "Two locales: ar (default, RTL) and en (LTR)" - documented closed set.
        RuleFor(x => x.PreferredLanguage)
            .NotEmpty()
            .Must(lang => lang is "ar" or "en")
            .WithMessage("PreferredLanguage must be 'ar' or 'en'.");
    }
}
