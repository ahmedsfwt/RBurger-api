using FluentValidation;

namespace RBurger.Application.Orders.Commands.CreateReview;

// Simple DTO/shape validation only (per the system prompt's separation of concerns).
// Business rules that need a database read (order existence, ownership, CustomerReceivedAt
// precondition, duplicate-review check) live in CreateReviewCommandHandler instead - exactly
// mirroring CreateOrderCommandValidator/CreateOrderCommandHandler's split.
public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        // §6.2 Reviews.Rating: tinyint, "1-5, required".
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);

        // §6.2 Reviews.Comment: nvarchar(500), nullable.
        RuleFor(x => x.Comment).MaximumLength(500).When(x => x.Comment is not null);
    }
}
