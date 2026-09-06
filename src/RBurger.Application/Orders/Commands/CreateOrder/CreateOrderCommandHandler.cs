using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IOrderRealtimeNotifier _realtimeNotifier;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IBranchRepository branchRepository,
        IMenuItemRepository menuItemRepository,
        IOrderRealtimeNotifier realtimeNotifier)
    {
        _orderRepository = orderRepository;
        _branchRepository = branchRepository;
        _menuItemRepository = menuItemRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "branch... doesn't exist".
        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null)
        {
            throw new NotFoundException($"Branch {request.BranchId} was not found.");
        }

        var catalogMenuItemIds = request.Items
            .Where(i => i.MenuItemId.HasValue)
            .Select(i => i.MenuItemId!.Value)
            .Distinct()
            .ToList();

        var menuItems = catalogMenuItemIds.Count > 0
            ? await _menuItemRepository.GetByIdsAsync(catalogMenuItemIds, cancellationToken)
            : new List<MenuItem>();
        var menuItemsById = menuItems.ToDictionary(mi => mi.Id);

        var orderItems = new List<OrderItem>();
        var subtotal = 0m;

        foreach (var itemDto in request.Items)
        {
            if (itemDto.MenuItemId.HasValue)
            {
                // §7.8: 404 "menu item... doesn't exist".
                if (!menuItemsById.TryGetValue(itemDto.MenuItemId.Value, out var menuItem))
                {
                    throw new NotFoundException($"Menu item {itemDto.MenuItemId.Value} was not found.");
                }

                // Approved decision #4: unavailable catalog items cannot be ordered.
                if (!menuItem.IsAvailable)
                {
                    throw new UnprocessableEntityException(
                        $"Menu item {menuItem.Id} is not currently available.",
                        "MENU_ITEM_UNAVAILABLE");
                }

                // Approved decision #5: catalog items must belong to the order's branch
                // (enforced because the approved Day 2 schema added MenuItem.BranchId).
                if (menuItem.BranchId != request.BranchId)
                {
                    throw new UnprocessableEntityException(
                        $"Menu item {menuItem.Id} does not belong to branch {request.BranchId}.",
                        "MENU_ITEM_BRANCH_MISMATCH");
                }

                // Approved decision #3: server-trusted price for catalog items - any
                // client-supplied unitPrice on this item is ignored entirely.
                var unitPrice = menuItem.Price;
                subtotal += unitPrice * itemDto.Quantity;

                orderItems.Add(new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    NameAr = menuItem.NameAr,
                    NameEn = menuItem.NameEn,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice
                });
            }
            else
            {
                // Custom burger - client-supplied unitPrice is trusted (approved decision #3);
                // CreateOrderItemDtoValidator already guarantees CustomName/UnitPrice are set.
                // §6.2 OrderItems has no description column, so CustomDescription (if sent) is
                // accepted but intentionally not persisted - matches the documented schema.
                var unitPrice = itemDto.UnitPrice!.Value;
                subtotal += unitPrice * itemDto.Quantity;

                orderItems.Add(new OrderItem
                {
                    MenuItemId = null,
                    NameAr = itemDto.CustomName!.Ar,
                    NameEn = itemDto.CustomName!.En,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice
                });
            }
        }

        // DeliveryFee is server-trusted, sourced from the Branch row - never client-supplied.
        var deliveryFee = branch.DeliveryFee;
        var total = subtotal + deliveryFee;

        var order = new Order
        {
            // Approved decision #6 (Day 1/2): client/application-generated GUID (ValueGeneratedNever).
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            CustomerId = request.CustomerId,
            DriverId = null, // §6.2: "nullable until Received"
            Stage = OrderStage.Confirmed, // §1.3/§6.2/§9.1: starts at Stage=0 (Confirmed)
            CustomerName = request.CustomerName,
            Phone = request.CustomerPhone,
            Address = request.DeliveryAddress,
            Notes = request.Notes,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Total = total,
            PaymentMethod = request.PaymentMethod,
            CustomerReceivedAt = null,
            // CreatedAt intentionally left unset - §6.2's documented DB default
            // (GETUTCDATE(), configured in OrderConfiguration) populates it on insert.
            OrderItems = orderItems,
            Payment = new Payment
            {
                // Approved decision #6 (Day 1/2): client/application-generated GUID.
                Id = Guid.NewGuid(),
                // §9.1: both cash and card payments start at Status=pending on order creation.
                Status = "pending",
                Method = request.PaymentMethod,
                Amount = total
            },
            // Day 8 approved decision #1: §6.2 documents OrderStatusEvents as the "audit
            // trail of every stage change", and TriggeredBy's documented closed set
            // (system │ driver │ customer) has no other place in the entire API where
            // "system" would ever be written - every §7.5 driver endpoint stamps "driver"
            // and the §9.4 admin refund path stamps "admin". Order creation (Stage=0
            // Confirmed) is therefore the one event this value exists to record. Added to
            // the navigation collection (not via IOrderRepository.AddOrderStatusEventAsync)
            // so it rides along in the same AddWithGeneratedOrderNumberAsync insert/retry as
            // the rest of this aggregate - no separate repository call or SaveChangesAsync
            // needed, and EF Core fixes up the OrderId FK from this navigation automatically,
            // exactly like OrderItems/Payment above. Timestamp is left unset - populated by
            // the same GETUTCDATE() DB default as every other OrderStatusEvent
            // (OrderStatusEventConfiguration), not by application code.
            OrderStatusEvents = new List<OrderStatusEvent>
            {
                new()
                {
                    Stage = OrderStage.Confirmed,
                    TriggeredBy = "system",
                    ActorId = null
                }
            }
        };

        await _orderRepository.AddWithGeneratedOrderNumberAsync(order, cancellationToken);

        // §8.1: "NewOrderAvailable(orderId, branchId) - pushed to all Drivers of that branch
        // when a new order reaches Stage=0 ... and also pushed to admin-branch-overview."
        // §8.3: DB write completes before broadcast; broadcast is best-effort. Only the
        // admin-branch-overview push is actually sent by the notifier implementation - see
        // IOrderRealtimeNotifier's XML comment and the Day 7 report for why the per-branch
        // Driver group is not broadcast to (its group name is not literally documented).
        await _realtimeNotifier.NotifyNewOrderAvailableAsync(order.Id, order.BranchId, cancellationToken);

        return new CreateOrderResponse
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Stage = order.Stage,
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            Total = order.Total,
            Payment = new PaymentSummaryDto
            {
                Status = order.Payment.Status,
                Method = order.Payment.Method
            },
            CreatedAt = order.CreatedAt
        };
    }
}
