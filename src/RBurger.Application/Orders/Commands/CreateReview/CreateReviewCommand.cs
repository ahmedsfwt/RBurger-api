using MediatR;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Commands.CreateReview;

// §7.4 POST /api/v1/orders/{orderId}/review request body: { "rating": 5, "comment": "..." }
public class CreateReviewCommand : IRequest<CreateReviewResponse>
{
    public int Rating { get; set; }
    public string? Comment { get; set; }

    // Not part of the documented JSON body - populated by OrdersController from the route
    // (orderId) and the authenticated JWT's "sub" claim (§5.4), the same pattern
    // CreateOrderCommand/CustomerReceivedCommand use.
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
}
