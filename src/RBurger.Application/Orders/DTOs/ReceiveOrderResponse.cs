using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.DTOs;

// §7.5 POST /api/v1/driver/orders/{orderId}/receive response:
// { "orderId":"9c41...", "stage":1, "driverId":"d91a...", "receivedAt":"..." }
public class ReceiveOrderResponse
{
    public Guid OrderId { get; set; }
    public OrderStage Stage { get; set; }
    public Guid DriverId { get; set; }
    public DateTime ReceivedAt { get; set; }
}
