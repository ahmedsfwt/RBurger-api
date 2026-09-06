namespace RBurger.Application.Orders.DTOs;

// §7.4: payment:{ "method": "cash", "status": "pending" } - identical shape reused by both
// the create-order response and the order-detail response.
public class PaymentSummaryDto
{
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
