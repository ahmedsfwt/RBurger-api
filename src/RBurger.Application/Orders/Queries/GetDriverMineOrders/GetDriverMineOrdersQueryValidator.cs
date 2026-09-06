using FluentValidation;

namespace RBurger.Application.Orders.Queries.GetDriverMineOrders;

// §7.5: "status=active│completed" - only these two literal values are documented. Any other
// value (including missing/empty) is a 400 VALIDATION_ERROR per §7.0/§7.8's existing
// ValidationBehavior pipeline, rather than silently defaulting or returning an empty list.
public class GetDriverMineOrdersQueryValidator : AbstractValidator<GetDriverMineOrdersQuery>
{
    public GetDriverMineOrdersQueryValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => status is "active" or "completed")
            .WithMessage("status must be 'active' or 'completed'.");
    }
}
