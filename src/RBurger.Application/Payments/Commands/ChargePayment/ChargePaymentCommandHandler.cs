using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Payments.DTOs;

namespace RBurger.Application.Payments.Commands.ChargePayment;

// §7.7/§9.3: "For paymentMethod=card only: creates a hosted-payment-page session with the
// gateway (§9.2) and returns the redirect URL; cash orders skip this call entirely."
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

        // §7.8: 403 "not the resource owner" - not literally re-stated for this specific
        // endpoint in §7.7, but applying the exact same ownership rule §7.4's other
        // Customer-JWT + order-id endpoints (customer-received, review) already enforce.
        if (order.CustomerId != request.CustomerId)
        {
            throw new ForbiddenException("This order does not belong to the calling customer.");
        }

        var payment = order.Payment;

        // §9.1/§9.3: this call only makes sense for paymentMethod=card - "cash orders skip
        // this call entirely." No documented status code is given for calling it on a cash
        // order, so this is rejected as a business-rule violation (422), consistent with
        // every other "wrong state for this action" case already established elsewhere
        // (e.g. §6.3's stage-transition rules).
        if (payment is null || payment.Method != "card")
        {
            throw new UnprocessableEntityException(
                "This order's payment method is not 'card'; charge sessions are only valid " +
                "for card payments.",
                "PAYMENT_METHOD_NOT_CARD");
        }

        // §9.2/EXTERNAL CONFIGURATION BLOCKER: NotConfiguredPaymentProvider always throws
        // here today - no Paymob/Fawry credentials exist in Documentation v1.2.
        var session = await _paymentProvider.CreateSessionAsync(order.Id, payment.Amount, "EGP");

        // §9.3: "API creates a Payments row (Status=pending) and calls the gateway to create
        // a hosted session" - Payment.Status was already set to "pending" at order-creation
        // time (CreateOrderCommandHandler) and is NOT changed here; §9.1's "authorized" state
        // is reached "at gateway redirect callback", a step with no documented RBurger
        // endpoint of its own, so no additional transition is applied at this step.
        return new ChargePaymentResponse
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            RedirectUrl = session.RedirectUrl
        };
    }
}
