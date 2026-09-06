using MediatR;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Payments.DTOs;

namespace RBurger.Application.Payments.Commands.ChargePayment;

// §7.7 POST /api/v1/payments/{orderId}/charge - Customer JWT · Idempotency-Key header required.
// Day 13: implements IIdempotentRequest - now actually deduplicated (see IdempotencyBehavior).
public record ChargePaymentCommand(Guid OrderId, Guid CustomerId, string? IdempotencyKey)
    : IRequest<ChargePaymentResponse>, IIdempotentRequest
{
    string IIdempotentRequest.IdempotencyEndpoint => "payments:charge";
    Guid IIdempotentRequest.IdempotencyScopeId => CustomerId;
    string IIdempotentRequest.IdempotencyFingerprint => $"{OrderId}|{CustomerId}";
}
