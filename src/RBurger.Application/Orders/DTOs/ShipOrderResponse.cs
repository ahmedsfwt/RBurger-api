using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.DTOs;

// §7.5 POST /api/v1/driver/orders/{orderId}/ship response:
// { "orderId":"9c41...", "stage":2, "shippedAt":"..." }
public class ShipOrderResponse
{
    public Guid OrderId { get; set; }
    public OrderStage Stage { get; set; }
    public DateTime ShippedAt { get; set; }
}
