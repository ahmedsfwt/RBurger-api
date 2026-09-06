using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Payments.DTOs;

namespace RBurger.Application.Payments.Commands.PaymentWebhook;

// §7.7/§9.3: "Server-to-server callback from the payment gateway confirming capture/failure;
// updates Payment.Status and, on success, broadcasts a PaymentConfirmed event over SignalR."
// §9.5: "the webhook endpoint additionally verifies the gateway's HMAC signature and rejects
// unsigned/invalid requests with 400."
public class PaymentWebhookCommandHandler
    : IRequestHandler<PaymentWebhookCommand, PaymentWebhookResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IOrderRealtimeNotifier _realtimeNotifier;

    public PaymentWebhookCommandHandler(
        IOrderRepository orderRepository,
        IPaymentProvider paymentProvider,
        IOrderRealtimeNotifier realtimeNotifier)
    {
        _orderRepository = orderRepository;
        _paymentProvider = paymentProvider;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<PaymentWebhookResponse> Handle(
        PaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        // §9.2/EXTERNAL CONFIGURATION BLOCKER: NotConfiguredPaymentProvider always throws
        // here today (mapped to 503) - no gateway webhook secret exists in Documentation v1.2
        // to actually validate an HMAC signature against. See §9.5's 400-for-invalid-signature
        // rule, which can only be honestly enforced once a real secret is configured.
        _paymentProvider.ValidateWebhookSignature(request.RawPayload, request.SignatureHeader ?? string.Empty);

        if (!Guid.TryParse(request.OrderReference, out var orderId))
        {
            throw new NotFoundException($"orderReference '{request.OrderReference}' is not a valid order id.");
        }

        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order?.Payment is null)
        {
            throw new NotFoundException($"Order {orderId} (or its payment) was not found.");
        }

        // §9.3: "on success" -> captured; "If the webhook reports failure, Payments.Status=failed".
        var succeeded = request.Status == "success";
        order.Payment.Status = succeeded ? "captured" : "failed";
        if (succeeded)
        {
            order.Payment.PaidAt = DateTime.UtcNow;
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);

        if (succeeded)
        {
            // §8.3/broadcast-after-DB-write convention already established for
            // OrderStatusChanged - the SaveChangesAsync above has already committed.
            await _realtimeNotifier.NotifyPaymentConfirmedAsync(
                order.Id, order.Payment.Status, cancellationToken);
        }

        return new PaymentWebhookResponse { Received = true };
    }
}
