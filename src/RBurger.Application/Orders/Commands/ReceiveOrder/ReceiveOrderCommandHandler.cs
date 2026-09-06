using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;
using RBurger.Domain.Entities;

namespace RBurger.Application.Orders.Commands.ReceiveOrder;

public class ReceiveOrderCommandHandler : IRequestHandler<ReceiveOrderCommand, ReceiveOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _realtimeNotifier;

    public ReceiveOrderCommandHandler(IOrderRepository orderRepository, IOrderRealtimeNotifier realtimeNotifier)
    {
        _orderRepository = orderRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<ReceiveOrderResponse> Handle(ReceiveOrderCommand request, CancellationToken cancellationToken)
    {
        // §6.3: "DriverId can only be set once (on Received) and cannot be reassigned
        // mid-flight; a second driver attempting to Receive an already-received order gets
        // 409 Conflict (§7)." Implemented as a single conditional UPDATE (WHERE DriverId IS
        // NULL) so two concurrent /receive calls on the same order cannot both succeed.
        var claimed = await _orderRepository.TryClaimForDriverAsync(
            request.OrderId, request.DriverId, cancellationToken);

        if (!claimed)
        {
            // rowsAffected == 0 means either the order doesn't exist, or it already has a
            // DriverId - a follow-up no-tracking existence check distinguishes 404 from 409.
            var exists = await _orderRepository.ExistsAsync(request.OrderId, cancellationToken);
            if (!exists)
            {
                throw new NotFoundException($"Order {request.OrderId} was not found.");
            }

            // §7.5: "Returns 409 Conflict if another driver already received it."
            throw new ConflictException(
                "This order has already been received by another driver.", "ORDER_ALREADY_RECEIVED");
        }

        // Re-read via a no-tracking query - TryClaimForDriverAsync updated the row directly
        // via SQL (ExecuteUpdateAsync), which bypasses the change tracker, so a tracked
        // re-query on this same DbContext could otherwise return stale cached values.
        var order = await _orderRepository.GetByIdNoTrackingAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            // Defensive only - TryClaimForDriverAsync just reported success against this id.
            throw new NotFoundException($"Order {request.OrderId} was not found.");
        }

        // §7.5: "appends an OrderStatusEvent". Appended unconditionally on a successful
        // receive, even when Stage was already 1 (kitchen had already started preparing) and
        // therefore does not numerically change - the audit trail still records that this
        // driver received the order at this moment.
        var statusEvent = new OrderStatusEvent
        {
            OrderId = order.Id,
            Stage = order.Stage,
            TriggeredBy = "driver",
            ActorId = request.DriverId
            // Timestamp: DB default GETUTCDATE() (OrderStatusEventConfiguration), populated
            // by EF Core after SaveChangesAsync - same pattern as Order.CreatedAt.
        };
        await _orderRepository.AddOrderStatusEventAsync(statusEvent, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // §8.3: DB write completes before broadcast; broadcast is best-effort.
        await _realtimeNotifier.NotifyOrderStatusChangedAsync(
            order.Id, order.Stage, statusEvent.Timestamp, "driver", cancellationToken);

        return new ReceiveOrderResponse
        {
            OrderId = order.Id,
            Stage = order.Stage,
            DriverId = request.DriverId,
            ReceivedAt = statusEvent.Timestamp
        };
    }
}
