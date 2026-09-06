using MediatR;
using RBurger.Application.Payments.DTOs;

namespace RBurger.Application.Payments.Commands.PaymentWebhook;

// §7.7 POST /api/v1/payments/webhook - "Gateway signature (HMAC) - not user-authenticated."
// RawPayload/SignatureHeader are needed for §9.5's HMAC verification
// (IPaymentProvider.ValidateWebhookSignature, §9.2); OrderReference/TransactionId/Status/Amount
// mirror §7.7's documented request JSON field-for-field.
public record PaymentWebhookCommand(
    string TransactionId,
    string OrderReference,
    string Status,
    decimal Amount,
    string RawPayload,
    string? SignatureHeader) : IRequest<PaymentWebhookResponse>;
