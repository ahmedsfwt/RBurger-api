using MediatR;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Commands.ShipOrder;

// §7.5 POST /api/v1/driver/orders/{orderId}/ship - Driver JWT (must be the assigned driver).
// No documented request body.
public class ShipOrderCommand : IRequest<ShipOrderResponse>
{
    public Guid OrderId { get; set; }

    // Not part of a documented body - populated by DriverOrdersController from the
    // authenticated JWT's "sub" claim (§5.4).
    public Guid DriverId { get; set; }
}
