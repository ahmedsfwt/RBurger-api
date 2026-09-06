using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Infrastructure.Realtime;

// §8.1: "Hub route: /hubs/orders". Mapped in RBurger.Api/Program.cs via
// app.MapHub<OrdersHub>("/hubs/orders"); JWT auth (access_token query-string support) is
// configured on the same JwtBearer scheme this Hub inherits via [Authorize] - see Program.cs.
//
// Day 15 (Backend Parity Spec §16): implements §8.2's three client-invoked group-join methods
// (JoinOrder/JoinBranch/JoinAdminOverview), previously deferred (Day 7) because no consuming
// client existed to verify authorization/lifecycle behavior against. Server-side authorization
// mirrors §8.2's prose exactly: "server authorizes (must own the order)" for Customers, an
// assigned/own-branch check for Drivers, and an Admin-role check for the overview group -
// enforced here so a connection can never join a group it isn't entitled to, regardless of
// what the (untrusted) client claims.
[Authorize]
public class OrdersHub : Hub
{
    // §8.1 Groups table's only two literally-named groups.
    private const string AdminOverviewGroup = "admin-branch-overview";

    // Day 15 addition: §8.1's Groups table documents that a per-branch Driver group exists
    // ("pushed to all Drivers of that branch") but never gives it a literal name (unlike
    // order-{orderId} and admin-branch-overview, both spelled out verbatim) - see
    // IOrderRealtimeNotifier's pre-existing XML comment, which is why that push was previously
    // left unsent. "branch-{branchId}" is an implementation decision, not a documented string:
    // applied consistently here and in SignalROrderRealtimeNotifier.NotifyNewOrderAvailableAsync
    // (updated in the same change), since implementing JoinBranch without also updating the
    // broadcast side would leave the feature connected to nothing.
    private static string BranchGroup(int branchId) => $"branch-{branchId}";
    private static string OrderGroup(Guid orderId) => $"order-{orderId}";

    private readonly IOrderRepository _orderRepository;
    private readonly IDriverRepository _driverRepository;

    public OrdersHub(IOrderRepository orderRepository, IDriverRepository driverRepository)
    {
        _orderRepository = orderRepository;
        _driverRepository = driverRepository;
    }

    // §8.2: "Customer opens the Tracking Modal for order X -> client calls JoinOrder(orderX)
    // -> server authorizes (must own the order) -> adds connection to group order-X." /
    // "Driver ... calls JoinOrder(orderId) for each order in their Active tab" (i.e. an order
    // already assigned to them). Unknown order or unauthorized caller: silently a no-op (no
    // group is joined) - mirrors §8.2's own description, which never mentions an error
    // response for this case, only "server authorizes".
    public async Task JoinOrder(Guid orderId)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId, Context.ConnectionAborted);
        if (order is null)
        {
            return;
        }

        var role = GetRole();
        var authorized = role switch
        {
            "Customer" => GetUserId() is { } customerId && order.CustomerId == customerId,
            "Driver" => GetUserId() is { } driverId && order.DriverId == driverId,
            "Admin" => true, // §8.2: "any order for an Admin"
            _ => false
        };

        if (authorized)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId));
        }
    }

    // §8.2: "Driver logs in -> client calls JoinBranch(branchId) to receive NewOrderAvailable
    // pushes." Authorized only for a Driver whose own assigned branch matches - a driver must
    // never be able to listen in on another branch's new-order stream (§24 IDOR concern).
    public async Task JoinBranch(int branchId)
    {
        if (GetRole() != "Driver" || GetUserId() is not { } driverId)
        {
            return;
        }

        var driver = await _driverRepository.GetByIdAsync(driverId, Context.ConnectionAborted);
        if (driver is not null && driver.BranchId == branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, BranchGroup(branchId));
        }
    }

    // §8.2: "Admin Dashboard session starts -> client calls JoinAdminOverview() -> server
    // verifies the Admin role and adds the connection to admin-branch-overview; this is the
    // only group an Admin JWT is allowed to join in bulk."
    public Task JoinAdminOverview()
    {
        if (GetRole() == "Admin")
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, AdminOverviewGroup);
        }

        return Task.CompletedTask;
    }

    private string? GetRole() => Context.User?.FindFirst("role")?.Value;

    private Guid? GetUserId()
    {
        var value = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
