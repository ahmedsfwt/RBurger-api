namespace RBurger.Application.Payments.DTOs;

// §7.7 POST /payments/{orderId}/charge response: { paymentId, status, redirectUrl }.
public class ChargePaymentResponse
{
    public Guid PaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
}

// §7.7 POST /payments/webhook response: { received: true }.
public class PaymentWebhookResponse
{
    public bool Received { get; set; }
}
