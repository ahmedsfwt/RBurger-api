namespace RBurger.Application.Common.Interfaces;

// §9.2: "The gateway is abstracted behind IPaymentProvider in RBurger.Application so the
// concrete provider can be swapped without touching controllers or DTOs." Method signatures
// copied verbatim from §9.2's C# interface listing - not re-derived or renamed.
//
// PaymentSession/RefundResult below are NOT literally documented as named types in §9.2 (the
// doc only shows the interface's method signatures, returning "PaymentSession"/"RefundResult"
// without listing their members). PaymentSession's two properties are taken directly from
// §7.7's documented POST /payments/{orderId}/charge response JSON ({ paymentId, status,
// redirectUrl }) - not invented, just the documented response shape given a settled internal
// name. RefundResult has no equivalent documented JSON anywhere (§9.4 describes the refund
// flow in prose only, with no response example), so it is kept to the minimum needed to let
// DeleteAdminOrderCommandHandler observe success/failure without inventing gateway-specific
// fields (transaction id, gateway payload, etc.) that no documented contract ever surfaces to
// a client.
public interface IPaymentProvider
{
    Task<PaymentSession> CreateSessionAsync(Guid orderId, decimal amount, string currency);
    Task<RefundResult> RefundAsync(Guid paymentId, decimal amount);
    bool ValidateWebhookSignature(string payload, string signatureHeader);
}

// §7.7 POST /payments/{orderId}/charge response: { paymentId, status, redirectUrl }. SessionId
// here corresponds to the response's "paymentId" (the Payments.Id already assigned when the
// order/payment row was created, per CreateOrderCommandHandler - not a new gateway-side id,
// since §9.3's sequence never mentions the gateway minting one before redirect).
public record PaymentSession(string SessionId, string RedirectUrl);

// No documented JSON response exists for §9.4's refund flow (prose-only description) - kept
// to the minimum an honest scaffold implementation needs to report failure vs success.
public record RefundResult(bool Success, string? GatewayReference);
