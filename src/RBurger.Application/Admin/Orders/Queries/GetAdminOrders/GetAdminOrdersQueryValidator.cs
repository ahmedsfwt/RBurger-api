using FluentValidation;
using RBurger.Domain.Enums;

namespace RBurger.Application.Admin.Orders.Queries.GetAdminOrders;

public class GetAdminOrdersQueryValidator : AbstractValidator<GetAdminOrdersQuery>
{
    public GetAdminOrdersQueryValidator()
    {
        // §1.3/§6.2: "Integer 0-3 lifecycle" - mirrors CreateReviewCommandValidator's
        // InclusiveBetween convention for the other documented closed integer range (rating
        // 1-5). Only validated when supplied (the filter itself is optional per §7.6.5).
        RuleFor(x => x.Stage)
            .Must(stage => stage is null || Enum.IsDefined(typeof(OrderStage), stage.Value))
            .WithMessage("Stage must be between 0 and 3.");

        RuleFor(x => x)
            .Must(x => x.DateFrom is null || x.DateTo is null || x.DateFrom <= x.DateTo)
            .WithMessage("dateFrom must not be after dateTo.");
    }
}
