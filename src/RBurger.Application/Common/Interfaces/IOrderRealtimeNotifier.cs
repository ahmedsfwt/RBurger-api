using RBurger.Domain.Enums;

namespace RBurger.Application.Common.Interfaces;

// Day 7 addition. §8: Application-layer abstraction over the real-time broadcast mechanism
// (SignalR) - Application must not reference ASP.NET Core SignalR types (Hub, IHubContext<T>)
// directly per the Clean Architecture rules. The concrete implementation lives in
// RBurger.Infrastructure and wraps IHubContext<OrdersHub>.
public interface IOrderRealtimeNotifier
{
    // §8.1: "Server -> Client event: OrderStatusChanged(orderId, stage, timestamp, triggeredBy)".
    // §8.2: published to group order-{orderId} and to admin-branch-overview, called from the
    // Application-layer command handler (not the controller) after the DB write commits (§8.3
    // - "the broadcast is best-effort UX, the database row is the source of truth").
    Task NotifyOrderStatusChangedAsync(
        Guid orderId,
        OrderStage stage,
        DateTime timestamp,
        string triggeredBy,
        CancellationToken cancellationToken);

    // §8.1: "Server -> Client event: NewOrderAvailable(orderId, branchId) - pushed to all
    // Drivers of that branch when a new order reaches Stage=0 ... and also pushed to
    // admin-branch-overview." Day 15 (Backend Parity Spec §16): now broadcast to both groups -
    // see SignalROrderRealtimeNotifier's XML comment for the "branch-{branchId}" naming
    // decision (not a literal string from §8.1, which never names this group explicitly; it is
    // now used consistently by both this broadcast and OrdersHub.JoinBranch).
    Task NotifyNewOrderAvailableAsync(Guid orderId, int branchId, CancellationToken cancellationToken);

    // ---- Day 12 addition (§7.7/§9.3 Payments) ----

    // §8.1: "Server -> Client event: PaymentConfirmed(orderId, status)". §9.3: "on success,
    // broadcasts PaymentConfirmed over SignalR (§8.1) so the client's Checkout screen can
    // transition to the Tracking Modal automatically." §8.1's Groups table does not repeat
    // which group this specific event targets (unlike OrderStatusChanged/NewOrderAvailable,
    // which are each explicitly cross-referenced to their groups) - broadcasting to
    // order-{orderId} is the literal, already-documented group for "this order's listeners"
    // (§3.5: the client "joins group order-{orderId} for every order currently visible"),
    // reusing the exact same group OrderStatusChanged already uses rather than inventing a
    // new one.
    Task NotifyPaymentConfirmedAsync(Guid orderId, string status, CancellationToken cancellationToken);
}
