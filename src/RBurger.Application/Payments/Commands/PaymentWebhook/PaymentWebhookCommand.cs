using MediatR;
using RBurger.Application.Payments.DTOs;

namespace RBurger.Application.Payments.Commands.PaymentWebhook;

// Paymob sends the transaction as JSON and the signature as the ?hmac= query parameter.
public record PaymentWebhookCommand(string RawPayload, string? Hmac) : IRequest<PaymentWebhookResponse>;