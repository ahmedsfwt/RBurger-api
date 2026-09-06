using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Admin.Orders.Commands.DeleteAdminOrder;

// §7.6.5 DELETE /api/v1/admin/orders/{id}: "Remove/cancel an order record ... Triggers a
// refund workflow (§9.4) if Payment.Status was captured."
//
// Day 13: the two branches previously blocked by the §6.2 schema (recorded in the Day 12
// report and OrderCancellationNotDocumentedException, now removed) are implemented using the
// approved schema additions Order.IsCancelled/CancelledAt and Payment.CashRefundNote/
// CashRefundNotedAt (see those properties' XML comments). A literal SQL DELETE of the Order
// row was never a valid reading of §9.4: OrderConfiguration deliberately sets Restrict on
// Order -> OrderItems/OrderStatusEvents/Payment/Review (an existing approved decision
// protecting the audit trail), and §9.4 itself proves the Order row must survive a refund (it
// explicitly appends a NEW OrderStatusEvents row referencing this same OrderId afterwards) -
// cancellation is a state change, not a row deletion, exactly like the rest of §9.4 describes.
public class DeleteAdminOrderCommandHandler : IRequestHandler<DeleteAdminOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentProvider _paymentProvider;

    public DeleteAdminOrderCommandHandler(
        IOrderRepository orderRepository, IPaymentProvider paymentProvider)
    {
        _orderRepository = orderRepository;
        _paymentProvider = paymentProvider;
    }

    public async Task Handle(DeleteAdminOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException($"Order {request.OrderId} was not found.");
        }

        // Defensive idempotency guard - not itemized separately in §7.8, but re-running this
        // action against an already-cancelled order (e.g. a client double-submit) must never
        // trigger a second refund attempt or overwrite CancelledAt. §7.8's existing 422
        // "business-rule violation" category is reused rather than inventing a new status code.
        if (order.IsCancelled)
        {
            throw new UnprocessableEntityException(
                "This order has already been cancelled.", "ORDER_ALREADY_CANCELLED");
        }

        var payment = order.Payment;

        if (payment is not null && payment.Status == "captured" && payment.Method == "card")
        {
            await HandleCapturedCardCancellationAsync(order, payment, request.AdminId, cancellationToken);
            return;
        }

        if (payment is not null && payment.Status == "captured" && payment.Method == "cash")
        {
            HandleCapturedCashCancellation(order, payment, request.AdminId);
            await _orderRepository.AddOrderStatusEventAsync(BuildCancellationEvent(order, request.AdminId), cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        // §9.4's "pending or authorized ... marked cancelled with no money movement" - also
        // covers a Payment row that is null/failed, since both leave no captured money to
        // refund and share the same "just cancel" outcome.
        CancelWithNoMoneyMovement(order);
        await _orderRepository.AddOrderStatusEventAsync(BuildCancellationEvent(order, request.AdminId), cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    // §9.4 CASE B (captured + card): "if it is captured, it calls IPaymentProvider.RefundAsync
    // ... for card payments ... On a successful gateway refund, Payments.Status is set to
    // refunded and an OrderStatusEvents row is appended with TriggeredBy=admin."
    //
    // Atomicity requirement ("do NOT mark an order cancelled if a required external card
    // refund fails"): RefundAsync is called and its result inspected BEFORE any local
    // mutation/SaveChanges, so a thrown exception (the NotConfiguredPaymentProvider scaffold
    // always throws today - §9.2/EXTERNAL INTEGRATION BLOCKER, out of scope for this task) or
    // an explicit Success == false response both leave the order/payment completely untouched.
    private async Task HandleCapturedCardCancellationAsync(
        Order order, Payment payment, Guid adminId, CancellationToken cancellationToken)
    {
        var refundResult = await _paymentProvider.RefundAsync(payment.Id, payment.Amount);

        if (!refundResult.Success)
        {
            throw new UnprocessableEntityException(
                "The card refund was declined by the payment gateway; the order was not cancelled.",
                "REFUND_FAILED");
        }

        // Reached only once a real gateway provider is configured and reports success -
        // unreachable with today's NotConfiguredPaymentProvider scaffold (it always throws
        // before returning), but implemented exactly per §9.4's fully-specified persistence
        // for this branch.
        payment.Status = "refunded";
        order.IsCancelled = true;
        order.CancelledAt = DateTime.UtcNow;

        await _orderRepository.AddOrderStatusEventAsync(BuildCancellationEvent(order, adminId), cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    // §9.4 CASE C (captured + cash): "or simply records a manual-refund note for cash payments
    // already collected." §9.4 never states that Payments.Status changes for this branch
    // (unlike the card branch, which explicitly says "set to refunded") - cash was genuinely
    // captured and no gateway movement occurs, so Status intentionally stays "captured"; only
    // the new CashRefundNote/CashRefundNotedAt columns and the order's cancellation flag change.
    private static void HandleCapturedCashCancellation(Order order, Payment payment, Guid adminId)
    {
        payment.CashRefundNote =
            $"Manual cash refund of {payment.Amount:F2} EGP recorded by Admin {adminId} on order cancellation.";
        payment.CashRefundNotedAt = DateTime.UtcNow;

        order.IsCancelled = true;
        order.CancelledAt = DateTime.UtcNow;
    }

    // §9.4 CASE A: "if it is still pending or authorized, it is instead marked cancelled with
    // no money movement."
    private static void CancelWithNoMoneyMovement(Order order)
    {
        order.IsCancelled = true;
        order.CancelledAt = DateTime.UtcNow;
    }

    // §9.4: "an OrderStatusEvents row is appended with TriggeredBy=admin (§6.2), preserving the
    // same audit trail used for driver-triggered stage changes" - applied to all three
    // cancellation branches, not just the card-refund one, since §9.4's closing paragraph
    // states the Admin Dashboard reflects "the refunded status" via the same mechanism "used
    // for order-status pushes" for the whole cancellation action, not only the card case.
    private static OrderStatusEvent BuildCancellationEvent(Order order, Guid adminId)
    {
        // OrderStatusEvent.Stage is a required, non-nullable "the stage transitioned to" column
        // (§6.2) - cancellation is not itself one of the documented 0-3 Stage transitions, so
        // the order's current (unchanged) Stage is recorded, the only value that doesn't invent
        // a new Stage meaning while still satisfying the column's NOT NULL constraint.
        return new OrderStatusEvent
        {
            OrderId = order.Id,
            Stage = order.Stage,
            TriggeredBy = "admin",
            ActorId = adminId,
            Timestamp = DateTime.UtcNow
        };
    }
}
