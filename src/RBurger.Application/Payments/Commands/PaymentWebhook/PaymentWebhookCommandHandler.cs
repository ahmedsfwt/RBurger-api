using System.Text.Json;
using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Payments.DTOs;

namespace RBurger.Application.Payments.Commands.PaymentWebhook;

// The webhook is the ONLY thing that moves a card payment to captured/failed.
// The SDK result inside the Flutter app is only a UI hint.
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
        // 1. Signature first. Must check the returned bool.
        if (string.IsNullOrWhiteSpace(request.Hmac)
            || !_paymentProvider.ValidateWebhookSignature(request.RawPayload, request.Hmac))
        {
            throw new InvalidWebhookSignatureException();
        }

        using var document = JsonDocument.Parse(request.RawPayload);
        var root = document.RootElement;

        // 2. Only transaction callbacks matter; ignore everything else and unfinished ones.
        JsonElement obj = default;
        var isTransaction =
            root.TryGetProperty("type", out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String
            && typeElement.GetString() == "TRANSACTION"
            && root.TryGetProperty("obj", out obj)
            && obj.ValueKind == JsonValueKind.Object;

        if (!isTransaction || GetBool(obj, "pending"))
        {
            return Ack();
        }

        // 3. Find our order through the special_reference we sent (= Order.Id).
        string? merchantOrderId = null;
        if (obj.TryGetProperty("order", out var orderElement) && orderElement.ValueKind == JsonValueKind.Object)
        {
            merchantOrderId = GetString(orderElement, "merchant_order_id");
        }

        if (!Guid.TryParse(merchantOrderId, out var orderId))
        {
            throw new NotFoundException($"merchant_order_id '{merchantOrderId}' is not a valid order id.");
        }

        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order?.Payment is null)
        {
            throw new NotFoundException($"Order {orderId} (or its payment) was not found.");
        }

        var payment = order.Payment;

        // 4. Duplicate / late webhook: never re-broadcast, never downgrade a paid order.
        if (payment.Status is "captured" or "refunded")
        {
            return Ack();
        }

        // 5. The money Paymob charged must match our DB amount.
        var expectedCents = (long)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
        var currency = GetString(obj, "currency");
        if (GetLong(obj, "amount_cents") != expectedCents
            || !string.Equals(currency, "EGP", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnprocessableEntityException(
                "The webhook amount/currency does not match the order's payment.",
                "PAYMENT_AMOUNT_MISMATCH");
        }

        // 6. Apply the result.
        var succeeded = GetBool(obj, "success")
                        && !GetBool(obj, "is_voided")
                        && !GetBool(obj, "is_refunded");

        payment.Status = succeeded ? "captured" : "failed";
        payment.GatewayProvider = "paymob";
        payment.GatewayTransactionId = GetIdText(obj);
        if (succeeded)
        {
            payment.PaidAt = DateTime.UtcNow;
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);

        // 7. Broadcast only after the DB write committed.
        if (succeeded)
        {
            await _realtimeNotifier.NotifyPaymentConfirmedAsync(order.Id, payment.Status, cancellationToken);
        }

        return Ack();
    }

    private static PaymentWebhookResponse Ack() => new() { Received = true };

    private static bool GetBool(JsonElement element, string name)
        => element.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.True;

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static long GetLong(JsonElement element, string name)
        => element.TryGetProperty(name, out var p)
           && p.ValueKind == JsonValueKind.Number
           && p.TryGetInt64(out var value)
            ? value
            : -1;

    private static string? GetIdText(JsonElement element)
    {
        if (!element.TryGetProperty("id", out var p))
        {
            return null;
        }

        return p.ValueKind == JsonValueKind.String ? p.GetString() : p.GetRawText();
    }
}