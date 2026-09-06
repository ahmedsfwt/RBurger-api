namespace RBurger.Application.Orders.DTOs;

// §7.4 GET /api/v1/orders/{orderId} response: "branch": { "nameAr": "...", "nameEn": "..." }
public class BranchSummaryDto
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

    // Day 14 addition (Backend Parity Spec §1.4): "Where the API currently exposes ETA
    // information, use the branch's EstimatedDeliveryTime value" - this is the order-level ETA
    // exposure point. Additive field; §7.4's two documented fields above are unchanged.
    public string? EstimatedDeliveryTime { get; set; }
}
