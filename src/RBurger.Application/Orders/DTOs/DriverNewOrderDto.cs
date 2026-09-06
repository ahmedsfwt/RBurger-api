namespace RBurger.Application.Orders.DTOs;

// §7.5 GET /api/v1/driver/orders/new response item (literal shape, bare array - approved
// Day 7 decision #2, no PagedResponse<T> envelope):
// { "orderId", "orderNumber", "customerAddress", "total", "notes" }
public class DriverNewOrderDto
{
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string CustomerAddress { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? Notes { get; set; }
}
