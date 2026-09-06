using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.DTOs;

// §7.4 POST /api/v1/orders response:
// { orderId, orderNumber, stage, subtotal, deliveryFee, total, payment:{status,method}, createdAt }
public class CreateOrderResponse
{
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }

    // Default System.Text.Json enum serialization (no JsonStringEnumConverter registered in
    // Program.cs) emits the underlying byte value, matching the documented "stage": 0 shape.
    public OrderStage Stage { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public PaymentSummaryDto Payment { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
