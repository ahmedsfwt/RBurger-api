using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;
using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.Commands.CustomerReceived;

public class CustomerReceivedCommandHandler : IRequestHandler<CustomerReceivedCommand, CustomerReceivedResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _realtimeNotifier;

    public CustomerReceivedCommandHandler(
        IOrderRepository orderRepository, IOrderRealtimeNotifier realtimeNotifier)
    {
        _orderRepository = orderRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<CustomerReceivedResponse> Handle(
        CustomerReceivedCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            // §7.8: 404 "Order/menu item/branch/driver id doesn't exist".
            throw new NotFoundException($"Order {request.OrderId} was not found.");
        }

        // §7.4 header row: "Customer JWT (must own the order, Stage must be 3)".
        // §7.8: 403 "Valid JWT but wrong role or not the resource owner".
        if (order.CustomerId != request.CustomerId)
        {
            throw new ForbiddenException("You do not have access to this order.");
        }

        // §7.4 header row + §6.3: "CustomerReceivedAt can only be set once Stage = 3."
        // Undocumented status code for this specific case (flagged in Phase 0 report) -
        // 422/UnprocessableEntityException is an implementation decision, approved for Day 5.
        if (order.Stage != OrderStage.Delivered)
        {
            throw new UnprocessableEntityException(
                "The order must reach Stage 3 (Delivered) before it can be marked as received.",
                "ORDER_NOT_DELIVERED");
        }

        // §6.3: "CustomerReceivedAt can only be set once." Undocumented status code for a
        // repeat call (flagged in Phase 0 report) - 409/ConflictException is an implementation
        // decision, approved for Day 5.
        if (order.CustomerReceivedAt is not null)
        {
            throw new ConflictException(
                "This order has already been marked as received.", "CUSTOMER_RECEIVED_ALREADY_SET");
        }

        // Approved Day 5 decision: application-set UTC timestamp (§7.0 "Dates are ISO-8601
        // UTC"), not a DB default - §6.2 documents no default for this nullable column, unlike
        // CreatedAt's GETUTCDATE().
        order.CustomerReceivedAt = DateTime.UtcNow;

        // Approved Day 5 decision (unchanged by Day 8): no OrderStatusEvent row - Stage does
        // not change here, it stays at 3 (Delivered).
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Day 8 approved decision #3: §7.4 states this endpoint "Fires OrderStatusChanged
        // over SignalR (§9) for consistency" - fully specified, so it is wired here now that
        // Hub infrastructure exists (Day 7). Broadcast only after the DB write commits (§8.3:
        // "the broadcast is best-effort UX, the database row is the source of truth"), same
        // ordering as every §7.5 driver handler. Stage is passed through unchanged (still
        // Delivered=3, per the Day 5 decision above) and triggeredBy="customer" - the one
        // documented TriggeredBy value (system │ driver │ customer) with no other call site
        // anywhere else in this codebase, since this is the only customer-initiated action
        // that reaches the notifier.
        await _realtimeNotifier.NotifyOrderStatusChangedAsync(
            order.Id, order.Stage, order.CustomerReceivedAt.Value, "customer", cancellationToken);

        return new CustomerReceivedResponse
        {
            OrderId = order.Id,
            CustomerReceivedAt = order.CustomerReceivedAt.Value
        };
    }
}
