using RBurger.Application.Admin.Branches.Commands.DeleteBranch;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using RBurger.Domain.Enums;
using Xunit;


namespace RBurger.Application.Tests.Admin;

public class DeleteBranchCommandHandlerTests
{
    private static Branch ExistingBranch() => new()
    {
        Id = 1,
        NameAr = "سوهاج",
        NameEn = "Sohag",
        DeliveryFee = 20,
        EtaMinMinutes = 25,
        EtaMaxMinutes = 35,
        IsActive = true
    };

    private static (DeleteBranchCommandHandler Handler, FakeBranchRepository Branches, FakeOrderRepository Orders, FakeDriverRepository Drivers)
        BuildHandler()
    {
        var branches = new FakeBranchRepository();
        var orders = new FakeOrderRepository();
        var drivers = new FakeDriverRepository();
        var handler = new DeleteBranchCommandHandler(branches, orders, drivers);
        return (handler, branches, orders, drivers);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_branch_does_not_exist()
    {
        var (handler, _, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteBranchCommand { Id = 999 }, default));
    }

    [Theory]
    [InlineData(OrderStage.Confirmed)]
    [InlineData(OrderStage.Preparing)]
    [InlineData(OrderStage.OnTheWay)]
    public async Task Handle_throws_UnprocessableEntityException_when_branch_has_non_terminal_order(OrderStage stage)
    {
        var (handler, branches, orders, _) = BuildHandler();
        branches.Branches.Add(ExistingBranch());
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, Stage = stage });

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new DeleteBranchCommand { Id = 1 }, default));
        Assert.Equal("BRANCH_HAS_NON_TERMINAL_ORDERS", ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_allows_deletion_when_branch_only_has_delivered_orders()
    {
        var (handler, branches, orders, _) = BuildHandler();
        branches.Branches.Add(ExistingBranch());
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), BranchId = 1, Stage = OrderStage.Delivered });

        await handler.Handle(new DeleteBranchCommand { Id = 1 }, default);

        Assert.Empty(branches.Branches);
    }

    [Fact]
    public async Task Handle_throws_UnprocessableEntityException_when_branch_has_assigned_driver()
    {
        var (handler, branches, _, drivers) = BuildHandler();
        branches.Branches.Add(ExistingBranch());
        drivers.Drivers.Add(new Driver { Id = Guid.NewGuid(), BranchId = 1, IsActive = true });

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new DeleteBranchCommand { Id = 1 }, default));
        Assert.Equal("BRANCH_HAS_ASSIGNED_DRIVERS", ex.ErrorCode);
    }

    // Approved interpretation (Day 10): "assigned drivers" means any Driver row FK'd to the
    // branch, regardless of IsActive.
    [Fact]
    public async Task Handle_throws_UnprocessableEntityException_even_when_the_assigned_driver_is_disabled()
    {
        var (handler, branches, _, drivers) = BuildHandler();
        branches.Branches.Add(ExistingBranch());
        drivers.Drivers.Add(new Driver { Id = Guid.NewGuid(), BranchId = 1, IsActive = false });

        await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new DeleteBranchCommand { Id = 1 }, default));
    }

    [Fact]
    public async Task Handle_deletes_branch_when_no_non_terminal_orders_and_no_drivers()
    {
        var (handler, branches, _, _) = BuildHandler();
        branches.Branches.Add(ExistingBranch());

        await handler.Handle(new DeleteBranchCommand { Id = 1 }, default);

        Assert.Empty(branches.Branches);
    }
}
