using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Payments.Commands.ChargePayment;
using RBurger.Application.Payments.Commands.PaymentWebhook;
using RBurger.Application.Payments.DTOs;

namespace RBurger.Api.Controllers;

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

    // POST /api/v1/payments/{orderId}/charge - Customer JWT, Idempotency-Key required.
    // Returns the Paymob clientSecret for the Flutter SDK.
    [HttpPost("{orderId:guid}/charge")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(ChargePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
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

    // POST /api/v1/payments/webhook - called by Paymob, not by a user.
    // The signature is the ?hmac= query parameter, and the body is Paymob's transaction JSON.
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaymentWebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PaymentWebhookResponse>> Webhook(
        [FromQuery(Name = "hmac")] string? hmac,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);

        var result = await _mediator.Send(
            new PaymentWebhookCommand(rawPayload, hmac), cancellationToken);

        return Ok(result);
    }
}