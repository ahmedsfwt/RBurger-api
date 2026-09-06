using RBurger.Application.Admin.Branches.Commands.UpdateBranch;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class UpdateBranchCommandHandlerTests
{
    private static Branch ExistingBranch() => new()
    {
        Id = 1,
        NameAr = "سوهاج",
        NameEn = "Sohag",
        DeliveryFee = 20,
        EtaMinMinutes = 25,
        EtaMaxMinutes = 35,
        HotlinePhones = "01000000000",
        IsActive = true
    };

    // §7.6.2's documented example: request { deliveryFee, isActive } -> only those change.
    [Fact]
    public async Task Handle_partially_updates_only_the_sent_fields()
    {
        var branches = new FakeBranchRepository();
        branches.Branches.Add(ExistingBranch());
        var handler = new UpdateBranchCommandHandler(branches);

        var result = await handler.Handle(new UpdateBranchCommand { Id = 1, DeliveryFee = 25, IsActive = true }, default);

        Assert.Equal(25, result.DeliveryFee);
        Assert.True(result.IsActive);
        Assert.Equal("سوهاج", result.NameAr); // unchanged - not an editable field
        Assert.Equal(25, result.EtaMinMinutes); // unchanged
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_branch_does_not_exist()
    {
        var branches = new FakeBranchRepository();
        var handler = new UpdateBranchCommandHandler(branches);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateBranchCommand { Id = 999, DeliveryFee = 10 }, default));
    }

    [Fact]
    public async Task Handle_throws_UnprocessableEntityException_when_only_new_min_exceeds_existing_max()
    {
        var branches = new FakeBranchRepository();
        branches.Branches.Add(ExistingBranch()); // Max = 35
        var handler = new UpdateBranchCommandHandler(branches);

        await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new UpdateBranchCommand { Id = 1, EtaMinMinutes = 40 }, default));
    }

    [Fact]
    public async Task Handle_allows_setting_both_eta_fields_to_a_valid_range()
    {
        var branches = new FakeBranchRepository();
        branches.Branches.Add(ExistingBranch());
        var handler = new UpdateBranchCommandHandler(branches);

        var result = await handler.Handle(
            new UpdateBranchCommand { Id = 1, EtaMinMinutes = 30, EtaMaxMinutes = 45 }, default);

        Assert.Equal(30, result.EtaMinMinutes);
        Assert.Equal(45, result.EtaMaxMinutes);
    }
}
