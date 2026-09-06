using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.Commands.DeliverOrder;

public class DeliverOrderCommandHandler : IRequestHandler<DeliverOrderCommand, DeliverOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _realtimeNotifier;

    public DeliverOrderCommandHandler(IOrderRepository orderRepository, IOrderRealtimeNotifier realtimeNotifier)
    {
        _orderRepository = orderRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<DeliverOrderResponse> Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        // Payment is needed for the §9.1 cash auto-capture side effect below, so the
        // richer (still lean-relative) GetByIdWithDetailsAsync is reused here instead of
        // adding a third bespoke Include shape - it already includes Payment.
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            // §7.8: 404 "Order... doesn't exist".
            throw new NotFoundException($"Order {request.OrderId} was not found.");
        }

        // §7.5 header row: "Driver JWT (must be the assigned driver)". §7.8: 403 "not the
        // resource owner".
        if (order.DriverId != request.DriverId)
        {
            throw new ForbiddenException("You are not the assigned driver for this order.");
        }

        // §6.3: forward-only stage transitions. /deliver advances Stage=2 (OnTheWay) to
        // Stage=3 (Delivered). Approved Day 7 decision #4: 422 for this business-rule
        // violation (out-of-sequence call).
        if (order.Stage != OrderStage.OnTheWay)
        {
            throw new UnprocessableEntityException(
                "The order must be at Stage 2 (On The Way) before it can be delivered.",
                "INVALID_ORDER_STAGE");
        }

        order.Stage = OrderStage.Delivered;

        // §9.1: "Cash on Delivery ... pending -> captured, auto-set by the
        // /driver/orders/{id}/deliver endpoint the moment the driver confirms delivery."
        // Only Payment.Status is touched here - §6.2's Payments.PaidAt has no documented
        // default or business rule stating when it is set (undocumented timestamp semantics,
        // flagged in the Day 7 report rather than invented), so it is intentionally left as-is.
        if (order.PaymentMethod == "cash" && order.Payment is not null && order.Payment.Status == "pending")
        {
            order.Payment.Status = "captured";
        }

        // §7.5: "appends an OrderStatusEvent, broadcasts OrderStatusChanged."
        var statusEvent = new OrderStatusEvent
        {
            OrderId = order.Id,
            Stage = order.Stage,
            TriggeredBy = "driver",
            ActorId = request.DriverId
        };
        await _orderRepository.AddOrderStatusEventAsync(statusEvent, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // §8.3: DB write completes before broadcast; broadcast is best-effort.
        await _realtimeNotifier.NotifyOrderStatusChangedAsync(
            order.Id, order.Stage, statusEvent.Timestamp, "driver", cancellationToken);

        return new DeliverOrderResponse
        {
            OrderId = order.Id,
            Stage = order.Stage,
            DeliveredAt = statusEvent.Timestamp
        };
    }
}
