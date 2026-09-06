namespace RBurger.Application.Payments.DTOs;

// §7.7 POST /payments/webhook request example:
// { "transactionId","orderReference","status","amount" }.
public class PaymentWebhookRequestDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string OrderReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
