using Microsoft.AspNetCore.SignalR;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Enums;

namespace RBurger.Infrastructure.Realtime;

// Concrete Infrastructure implementation of the Application-layer IOrderRealtimeNotifier
// abstraction (§8), wrapping IHubContext<OrdersHub> so Application never references SignalR.
public class SignalROrderRealtimeNotifier : IOrderRealtimeNotifier
{
    // §8.1 Groups table - the only group name literally documented for Admin Dashboard
    // sessions.
    private const string AdminOverviewGroup = "admin-branch-overview";

    // Day 15 (Backend Parity Spec §16) - see OrdersHub's identical constant/comment. Kept in
    // sync between the two files by construction (same "branch-{branchId}" literal), since
    // there is no shared constants file between Hub and notifier in this project's structure.
    private static string BranchGroup(int branchId) => $"branch-{branchId}";

    private readonly IHubContext<OrdersHub> _hubContext;

    public SignalROrderRealtimeNotifier(IHubContext<OrdersHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyOrderStatusChangedAsync(
        Guid orderId, OrderStage stage, DateTime timestamp, string triggeredBy, CancellationToken cancellationToken)
    {
        // §8.1: "OrderStatusChanged(orderId, stage, timestamp, triggeredBy)".
        // §8.2: "publish OrderStatusChanged to group order-{orderId} and to
        // admin-branch-overview via IHubContext<OrdersHub>" - both groups are literally
        // named in §8.1's Groups table, so both are broadcast to here.
        var payload = new { orderId, stage, timestamp, triggeredBy };

        await _hubContext.Clients.Group($"order-{orderId}")
            .SendAsync("OrderStatusChanged", payload, cancellationToken);
        await _hubContext.Clients.Group(AdminOverviewGroup)
            .SendAsync("OrderStatusChanged", payload, cancellationToken);
    }

    public async Task NotifyNewOrderAvailableAsync(Guid orderId, int branchId, CancellationToken cancellationToken)
    {
        // §8.1: "NewOrderAvailable(orderId, branchId) - pushed to all Drivers of that branch
        // ... and also pushed to admin-branch-overview." Day 15: now also broadcast to
        // branch-{branchId} (see this class's XML comment on BranchGroup) now that OrdersHub's
        // JoinBranch actually populates that group - previously only the admin-branch-overview
        // push was sent, since no client could join the per-branch group at all.
        var payload = new { orderId, branchId };

        await _hubContext.Clients.Group(AdminOverviewGroup)
            .SendAsync("NewOrderAvailable", payload, cancellationToken);
        await _hubContext.Clients.Group(BranchGroup(branchId))
            .SendAsync("NewOrderAvailable", payload, cancellationToken);
    }

    // ---- Day 12 addition (§7.7/§9.3 Payments) ----

    public async Task NotifyPaymentConfirmedAsync(
        Guid orderId, string status, CancellationToken cancellationToken)
    {
        var payload = new { orderId, status };

        await _hubContext.Clients.Group($"order-{orderId}")
            .SendAsync("PaymentConfirmed", payload, cancellationToken);
    }
}
