using RBurger.Application.Admin.Branches.Commands.CreateBranch;
using RBurger.Application.Tests.Orders;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class CreateBranchCommandHandlerTests
{
    private static CreateBranchCommand ValidCommand() => new()
    {
        NameAr = "أسيوط",
        NameEn = "Assiut",
        DeliveryFee = 22,
        EtaMinMinutes = 25,
        EtaMaxMinutes = 40
    };

    [Fact]
    public async Task Handle_creates_branch_and_returns_documented_shape()
    {
        var branches = new FakeBranchRepository();
        var handler = new CreateBranchCommandHandler(branches);

        var result = await handler.Handle(ValidCommand(), default);

        Assert.Single(branches.Branches);
        Assert.Equal("أسيوط", result.NameAr);
        Assert.Equal("Assiut", result.NameEn);
        Assert.Equal(22, result.DeliveryFee);
        Assert.True(result.IsActive); // §6.2: default 1
        Assert.Equal(25, result.EtaMinMinutes);
        Assert.Equal(40, result.EtaMaxMinutes);
    }
}
