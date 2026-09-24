using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Payments.DTOs;

namespace RBurger.Application.Payments.Commands.ChargePayment;

// §7.7/§9.3: creates the gateway session for a card order. With Paymob this returns the
// clientSecret that the Flutter SDK needs.
public class ChargePaymentCommandHandler
    : IRequestHandler<ChargePaymentCommand, ChargePaymentResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentProvider _paymentProvider;

    public ChargePaymentCommandHandler(
        IOrderRepository orderRepository, IPaymentProvider paymentProvider)
    {
        _orderRepository = orderRepository;
        _paymentProvider = paymentProvider;
    }

    public async Task<ChargePaymentResponse> Handle(
        ChargePaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException($"Order {request.OrderId} was not found.");
        }

        if (order.CustomerId != request.CustomerId)
        {
            throw new ForbiddenException("This order does not belong to the calling customer.");
        }

        var payment = order.Payment;

        if (payment is null || payment.Method != "card")
        {
            throw new UnprocessableEntityException(
                "This order's payment method is not 'card'; charge sessions are only valid " +
                "for card payments.",
                "PAYMENT_METHOD_NOT_CARD");
        }

        if (order.IsCancelled)
        {
            throw new UnprocessableEntityException(
                "This order has been cancelled.", "ORDER_CANCELLED");
        }

        // Retry is allowed while pending or failed; a paid order can never be charged again.
        if (payment.Status is "captured" or "refunded")
        {
            throw new UnprocessableEntityException(
                "This order has already been paid.", "PAYMENT_ALREADY_CAPTURED");
        }

        // The amount always comes from the DB (payment.Amount), never from the client.
        var session = await _paymentProvider.CreateSessionAsync(order.Id, payment.Amount, "EGP");

        payment.GatewayProvider = "paymob";
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return new ChargePaymentResponse
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            RedirectUrl = session.RedirectUrl,
            ClientSecret = session.ClientSecret
        };
    }
}