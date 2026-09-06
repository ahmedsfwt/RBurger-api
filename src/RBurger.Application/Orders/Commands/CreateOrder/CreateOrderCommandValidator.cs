using FluentValidation;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Commands.CreateOrder;

// Simple DTO/shape validation only (per the system prompt's separation of concerns).
// Business rules that need a database read (branch/menu-item existence, availability,
// branch-scoping, server-trusted pricing) live in CreateOrderCommandHandler instead.
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);

        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemDtoValidator());

        // §6.2 Orders.CustomerName is a snapshot column; mirrors Customers.FullName's
        // documented max length (150) since §6.2 gives no separate length for the snapshot.
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);

        // §6.2 Orders.Phone snapshot column; mirrors Customers.Phone's max length (20).
        RuleFor(x => x.CustomerPhone).NotEmpty().MaximumLength(20);

        // §6.2 Orders.Address snapshot column; mirrors Customers.DefaultAddress's max length (300).
        RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(300);

        // §6.2 Orders.Notes nvarchar(500), nullable.
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);

        // §6.2 Orders.PaymentMethod nvarchar(10), documented closed set "cash | card".
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(m => m is "cash" or "card")
            .WithMessage("PaymentMethod must be 'cash' or 'card'.");

        // Approved decision #1: Idempotency-Key header required on POST /orders (§5.5, §7.4).
        // Validated for presence only - no persistence/dedup mechanism at this stage.
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("The Idempotency-Key header is required.");
    }
}

public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);

        // §7.4 request example: a custom burger item (menuItemId: null) carries
        // customName + unitPrice; a catalog item (menuItemId set) carries neither.
        When(x => x.MenuItemId is null, () =>
        {
            RuleFor(x => x.CustomName).NotNull()
                .WithMessage("CustomName is required for custom (menuItemId: null) items.");

            // Written as null-safe .Must() predicates (rather than a chained
            // CustomName!.Ar/.En member-access RuleFor) so the rule never dereferences
            // CustomName when it is null, regardless of FluentValidation's internal
            // evaluation order for the accessor vs. the NotNull rule above.
            RuleFor(x => x.CustomName)
                .Must(cn => cn is null || !string.IsNullOrWhiteSpace(cn.Ar))
                .WithMessage("CustomName.Ar is required for custom (menuItemId: null) items.");

            RuleFor(x => x.CustomName)
                .Must(cn => cn is null || !string.IsNullOrWhiteSpace(cn.En))
                .WithMessage("CustomName.En is required for custom (menuItemId: null) items.");

            RuleFor(x => x.UnitPrice)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("UnitPrice is required for custom (menuItemId: null) items.");
        });
    }
}
