using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.DTOs;

// §7.5 POST /api/v1/driver/orders/{orderId}/deliver response:
// { "orderId":"9c41...", "stage":3, "deliveredAt":"..." }
public class DeliverOrderResponse
{
    public Guid OrderId { get; set; }
    public OrderStage Stage { get; set; }
    public DateTime DeliveredAt { get; set; }
}
