using MediatR;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Queries.GetDriverNewOrders;

// §7.5 GET /api/v1/driver/orders/new - Driver JWT.
public class GetDriverNewOrdersQuery : IRequest<List<DriverNewOrderDto>>
{
    // Not part of a documented query param - populated by DriverOrdersController from the
    // authenticated JWT's "sub" claim (§5.4), same pattern as every other Day 3-6 handler.
    public Guid DriverId { get; set; }
}
