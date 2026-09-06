using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.DTOs;

// §7.5 GET /api/v1/driver/orders/mine?status=active|completed response item (literal shape,
// bare array - approved Day 7 decision #2, no PagedResponse<T> envelope):
// { "orderId", "orderNumber", "stage", "customerAddress", "total" }
public class DriverOrderMineDto
{
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public OrderStage Stage { get; set; }
    public string CustomerAddress { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
