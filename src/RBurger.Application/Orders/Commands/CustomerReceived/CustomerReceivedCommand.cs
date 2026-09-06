using MediatR;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Commands.CustomerReceived;

// §7.4 POST /api/v1/orders/{orderId}/customer-received - no request body documented, only the
// orderId path parameter and the Customer JWT's ownership check.
public class CustomerReceivedCommand : IRequest<CustomerReceivedResponse>
{
    public Guid OrderId { get; set; }

    // Not part of the documented JSON body - populated by OrdersController from the
    // authenticated JWT's "sub" claim (§5.4), the same pattern CreateOrderCommand uses.
    public Guid CustomerId { get; set; }
}
