namespace RBurger.Application.Orders.DTOs;

// §7.4 POST /api/v1/orders/{orderId}/customer-received response:
// { "orderId":"9c41...", "customerReceivedAt":"2026-07-29T15:10:00Z" }
public class CustomerReceivedResponse
{
    public Guid OrderId { get; set; }
    public DateTime CustomerReceivedAt { get; set; }
}
