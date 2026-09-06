using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Payments.Commands.ChargePayment;
using RBurger.Application.Payments.Commands.PaymentWebhook;
using RBurger.Application.Payments.DTOs;

namespace RBurger.Api.Controllers;

// §7.7 Payments. §5.5 (Backend Parity Spec §13): fixed-window rate limiting on every Payment
// endpoint, including the webhook - a flood of forged webhook calls is exactly the kind of
// traffic this policy exists to blunt, on top of its own HMAC signature check.
[ApiController]
[Route("api/v1/payments")]
[EnableRateLimiting("payment")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public PaymentsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    // §7.7 POST /api/v1/payments/{orderId}/charge - Customer JWT · Idempotency-Key header
    // required. Method-level [Authorize] (not class-level), mirroring OrdersController's
    // documented reasoning: this controller also hosts the webhook action below, which must
    // remain anonymous/public - a class-level attribute would incorrectly also gate it.
    [HttpPost("{orderId:guid}/charge")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(ChargePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ChargePaymentResponse>> Charge(
        Guid orderId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var customerId = _currentUserService.CustomerId!.Value;
        var result = await _mediator.Send(
            new ChargePaymentCommand(orderId, customerId, idempotencyKey), cancellationToken);
        return Ok(result);
    }

    // §7.7 POST /api/v1/payments/webhook - "Gateway signature (HMAC) - not user-authenticated."
    // Deliberately [AllowAnonymous]: no JWT is ever presented by the gateway, only the HMAC
    // signature header, per §9.5.
    //
    // The exact HMAC header name is never given anywhere in Documentation v1.2 (§7.7/§9.2/§9.5
    // all describe "the gateway's HMAC signature" only in prose) - "X-Signature" below is a
    // wiring placeholder with zero effect on behavior today, since NotConfiguredPaymentProvider
    // throws before this value is ever inspected regardless of what is sent. Once a real
    // Paymob/Fawry integration is configured, this header name must be confirmed against that
    // provider's actual webhook contract - flagged in the Day 12 report.
    //
    // Similarly, RawPayload below is the JSON re-serialized from the already-model-bound DTO
    // rather than the original request bytes (ASP.NET Core's model binder consumes the body
    // stream before this action runs) - functionally identical today since the scaffold
    // provider never inspects it, but a real HMAC check needs the exact original bytes; also
    // flagged in the Day 12 report as something to revisit alongside the header name above.
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaymentWebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PaymentWebhookResponse>> Webhook(
        [FromBody] PaymentWebhookRequestDto body,
        [FromHeader(Name = "X-Signature")] string? signatureHeader,
        CancellationToken cancellationToken)
    {
        var rawPayload = JsonSerializer.Serialize(body);

        var result = await _mediator.Send(
            new PaymentWebhookCommand(
                body.TransactionId, body.OrderReference, body.Status, body.Amount,
                rawPayload, signatureHeader),
            cancellationToken);

        return Ok(result);
    }
}
