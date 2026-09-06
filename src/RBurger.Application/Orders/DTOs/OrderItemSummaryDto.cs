namespace RBurger.Application.Orders.DTOs;

// §7.4 GET /api/v1/orders/{orderId} response: "items":[{"nameAr":"...","quantity":2,"unitPrice":90}]
// Sourced directly from the OrderItem snapshot columns (§6.2) - no MenuItem join needed.
public class OrderItemSummaryDto
{
    public string NameAr { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
