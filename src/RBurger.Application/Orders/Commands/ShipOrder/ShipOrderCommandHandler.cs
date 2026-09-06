using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.Commands.ShipOrder;

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, ShipOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _realtimeNotifier;

    public ShipOrderCommandHandler(IOrderRepository orderRepository, IOrderRealtimeNotifier realtimeNotifier)
    {
        _orderRepository = orderRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<ShipOrderResponse> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
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

        // §6.3: forward-only stage transitions. /ship advances Stage=1 (Preparing/received) to
        // Stage=2 (OnTheWay). Approved Day 7 decision #4: 422 for this business-rule violation
        // (out-of-sequence call), reserving 409 strictly for the documented /receive race.
        if (order.Stage != OrderStage.Preparing)
        {
            throw new UnprocessableEntityException(
                "The order must be at Stage 1 (Preparing) before it can be shipped.",
                "INVALID_ORDER_STAGE");
        }

        order.Stage = OrderStage.OnTheWay;

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

        return new ShipOrderResponse
        {
            OrderId = order.Id,
            Stage = order.Stage,
            ShippedAt = statusEvent.Timestamp
        };
    }
}
