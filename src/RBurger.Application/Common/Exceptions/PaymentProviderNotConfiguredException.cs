namespace RBurger.Application.Common.Exceptions;

// Day 12 addition, mirroring StorageNotConfiguredException's exact Day 10 precedent (approved
// Blocking Issue #2 pattern, reused here for Blocking Issue #3 per Ahmed's Day 12 approval).
// §9.2 documents IPaymentProvider's contract (CreateSessionAsync/RefundAsync/
// ValidateWebhookSignature) but Documentation v1.2 never specifies the concrete Paymob/Fawry
// bucket-equivalent - API key, merchant id, webhook secret, or any other gateway credential.
// Thrown by NotConfiguredPaymentProvider (Infrastructure) whenever POST /payments/{orderId}/
// charge (§7.7), the webhook handler (§7.7), or the DELETE /admin/orders/{id} card-refund path
// (§9.4) actually needs to reach the gateway. NOT a documented §7.8 status code - an honest
// "feature not yet deployed" signal (503), distinct from every documented client/business
// error, exactly like StorageNotConfiguredException.
public class PaymentProviderNotConfiguredException : Exception
{
    public PaymentProviderNotConfiguredException()
        : base("Payment gateway integration is not yet configured in this environment.")
    {
    }
}
