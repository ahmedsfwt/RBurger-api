using FluentValidation;

namespace RBurger.Application.Payments.Commands.ChargePayment;

public class ChargePaymentCommandValidator : AbstractValidator<ChargePaymentCommand>
{
    public ChargePaymentCommandValidator()
    {
        // §5.5: "Idempotency-Key header required on ... POST /payments/charge" - validated
        // for presence only, exactly mirroring CreateOrderCommandValidator's identical rule
        // (approved decision #1's "validated for presence only, no persistence/dedup" scope).
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("The Idempotency-Key header is required.");
    }
}
