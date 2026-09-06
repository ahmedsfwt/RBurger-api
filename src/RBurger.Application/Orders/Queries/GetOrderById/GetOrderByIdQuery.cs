using MediatR;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Queries.GetOrderById;

// §7.4 GET /api/v1/orders/{orderId} - Customer JWT (own order) or Driver JWT (assigned order).
// Day 7 addition: completes the "or Driver JWT (assigned order)" branch that was deferred in
// Day 4 pending Driver authentication (added in Day 6). Both requester ids are nullable and
// mutually exclusive in practice - DriverOrdersController/OrdersController populate exactly
// one of them from the authenticated JWT's role/sub claims (§5.4).
public class GetOrderByIdQuery : IRequest<OrderDetailResponse>
{
    public Guid OrderId { get; set; }
    public Guid? RequestingCustomerId { get; set; }
    public Guid? RequestingDriverId { get; set; }
}
