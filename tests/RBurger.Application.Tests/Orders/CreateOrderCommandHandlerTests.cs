using RBurger.Application.Common.Exceptions;
using RBurger.Application.Orders.Commands.CreateOrder;
using RBurger.Application.Orders.DTOs;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;

namespace RBurger.Application.Tests.Orders;

public class CreateOrderCommandHandlerTests
{
    private static Branch SohagBranch() => new()
    {
        Id = 1,
        NameAr = "سوهاج",
        NameEn = "Sohag",
        DeliveryFee = 20m,
        EtaMinMinutes = 25,
        EtaMaxMinutes = 35,
        HotlinePhones = "01000000000",
        IsActive = true
    };

    private static MenuItem AvailableCatalogItem(int id = 101, int branchId = 1, decimal price = 90m) => new()
    {
        Id = id,
        CategoryId = 1,
        NameAr = "أورجينال",
        NameEn = "Original",
        DescriptionAr = "...",
        DescriptionEn = "...",
        Price = price,
        BranchId = branchId,
        IsAvailable = true
    };

    private static CreateOrderCommand ValidCommand() => new()
    {
        BranchId = 1,
        CustomerId = Guid.NewGuid(),
        IdempotencyKey = "a-unique-key",
        CustomerName = "Ahmed Sami",
        CustomerPhone = "01012345678",
        DeliveryAddress = "Sohag, University street",
        Notes = "No pickles please",
        PaymentMethod = "cash",
        Items = new List<CreateOrderItemDto>
        {
            new() { MenuItemId = 101, Quantity = 2 }
        }
    };

    private static (CreateOrderCommandHandler Handler, FakeOrderRepository Orders, FakeBranchRepository Branches, FakeMenuItemRepository MenuItems, FakeOrderRealtimeNotifier RealtimeNotifier)
        BuildHandler()
    {
        var orders = new FakeOrderRepository();
        var branches = new FakeBranchRepository();
        var menuItems = new FakeMenuItemRepository();
        var realtimeNotifier = new FakeOrderRealtimeNotifier();
        var handler = new CreateOrderCommandHandler(orders, branches, menuItems, realtimeNotifier);
        return (handler, orders, branches, menuItems, realtimeNotifier);
    }

    [Fact]
    public async Task Should_ignore_client_unitPrice_and_use_MenuItem_Price_for_catalog_items()
    {
        // Approved decision #3.
        var (handler, _, branches, menuItems, _) = BuildHandler();
        branches.Branches.Add(SohagBranch());
        menuItems.MenuItems.Add(AvailableCatalogItem(price: 90m));

        var command = ValidCommand();
        command.Items = new List<CreateOrderItemDto>
        {
            new() { MenuItemId = 101, Quantity = 2, UnitPrice = 9999m } // must be ignored
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(180m, result.Subtotal); // 2 * 90, NOT 2 * 9999
        Assert.Equal(200m, result.Total); // subtotal + Sohag DeliveryFee(20)
    }

    [Fact]
    public async Task Should_trust_client_unitPrice_for_custom_items()
    {
        // Approved decision #3.
        var (handler, _, branches, _, _) = BuildHandler();
        branches.Branches.Add(SohagBranch());

        var command = ValidCommand();
        command.Items = new List<CreateOrderItemDto>
        {
            new()
            {
                MenuItemId = null,
                Quantity = 1,
                CustomName = new LocalizedTextDto { Ar = "برجرك المميز", En = "Your Signature Burger" },
                UnitPrice = 145m
            }
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(145m, result.Subtotal);
    }

    [Fact]
    public async Task Should_reject_unavailable_menu_item_with_UnprocessableEntityException()
    {
        // Approved decision #4.
        var (handler, _, branches, menuItems, _) = BuildHandler();
        branches.Branches.Add(SohagBranch());
        var unavailableItem = AvailableCatalogItem();
        unavailableItem.IsAvailable = false;
        menuItems.MenuItems.Add(unavailableItem);

        var command = ValidCommand();

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal("MENU_ITEM_UNAVAILABLE", ex.ErrorCode);
    }

    [Fact]
    public async Task Should_reject_menu_item_from_a_different_branch_with_UnprocessableEntityException()
    {
        // Approved decision #5.
        var (handler, _, branches, menuItems, _) = BuildHandler();
        branches.Branches.Add(SohagBranch());
        menuItems.MenuItems.Add(AvailableCatalogItem(branchId: 2)); // Girga, not Sohag

        var command = ValidCommand();

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal("MENU_ITEM_BRANCH_MISMATCH", ex.ErrorCode);
    }

    [Fact]
    public async Task Should_throw_NotFoundException_when_branch_does_not_exist()
    {
        var (handler, _, _, _, _) = BuildHandler();
        var command = ValidCommand();

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_NotFoundException_when_menu_item_does_not_exist()
    {
        var (handler, _, branches, _, _) = BuildHandler();
        branches.Branches.Add(SohagBranch());
        // No menu items registered in the fake repository.

        var command = ValidCommand();

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_set_DeliveryFee_from_branch_and_ignore_any_notion_of_client_value()
    {
        var (handler, _, branches, menuItems, _) = BuildHandler();
        branches.Branches.Add(SohagBranch()); // DeliveryFee = 20
        menuItems.MenuItems.Add(AvailableCatalogItem());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(20m, result.DeliveryFee);
    }

    [Fact]
    public async Task Should_start_order_at_Stage_Confirmed_with_pending_payment()
    {
        var (handler, _, branches, menuItems, _) = BuildHandler();
        branches.Branches.Add(SohagBranch());
        menuItems.MenuItems.Add(AvailableCatalogItem());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(OrderStage.Confirmed, result.Stage);
        Assert.Equal("pending", result.Payment.Status);
        Assert.Equal("cash", result.Payment.Method);
    }

    [Fact]
    public async Task Should_persist_the_order_via_the_repository()
    {
        var (handler, orders, branches, menuItems, _) = BuildHandler();
        branches.Branches.Add(SohagBranch());
        menuItems.MenuItems.Add(AvailableCatalogItem());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Single(orders.AddedOrders);
        Assert.Equal(result.OrderId, orders.AddedOrders[0].Id);
    }

    // ---- Day 8 approved decision #1 ----

    [Fact]
    public async Task Should_append_a_system_OrderStatusEvent_at_Stage_Confirmed_on_creation()
    {
        var (handler, orders, branches, menuItems, _) = BuildHandler();
        branches.Branches.Add(SohagBranch());
        menuItems.MenuItems.Add(AvailableCatalogItem());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        var persistedOrder = orders.AddedOrders.Single(o => o.Id == result.OrderId);
        var statusEvent = Assert.Single(persistedOrder.OrderStatusEvents);
        Assert.Equal(OrderStage.Confirmed, statusEvent.Stage);
        Assert.Equal("system", statusEvent.TriggeredBy);
        Assert.Null(statusEvent.ActorId);
        // Simulated GETUTCDATE() default - confirms the fake populated it, not the handler.
        Assert.NotEqual(default, statusEvent.Timestamp);
    }

    [Fact]
    public async Task Should_not_broadcast_OrderStatusChanged_on_creation_only_NewOrderAvailable()
    {
        // Day 8 does not add a status-changed broadcast for order creation - only the
        // already-approved (Day 7) NewOrderAvailable push. The system OrderStatusEvent row
        // (approved decision #1) is a persistence-only audit concern, kept separate from
        // realtime notification semantics per the Day 8 brief.
        var (handler, _, branches, menuItems, realtimeNotifier) = BuildHandler();
        branches.Branches.Add(SohagBranch());
        menuItems.MenuItems.Add(AvailableCatalogItem());

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Empty(realtimeNotifier.StatusChangedCalls);
        Assert.Single(realtimeNotifier.NewOrderAvailableCalls);
    }
}
