using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.DTOs;

// §7.4 GET /api/v1/orders/mine response item:
// { orderId, orderNumber, branchNameAr, stage, total, customerReceivedAt, hasReview }
public class OrdersMineItemDto
{
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string BranchNameAr { get; set; } = string.Empty;
    public OrderStage Stage { get; set; }
    public decimal Total { get; set; }
    public DateTime? CustomerReceivedAt { get; set; }
    public bool HasReview { get; set; }
}
