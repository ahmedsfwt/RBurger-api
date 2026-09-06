using RBurger.Application.Admin.Menu.Commands.CreateMenuItem;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class CreateMenuItemCommandHandlerTests
{
    private static MenuCategory BurgersCategory() => new() { Id = 1, Key = "burgers", LabelAr = "البرجر", LabelEn = "Burgers", SortOrder = 1 };
    private static Branch SohagBranch() => new() { Id = 1, NameAr = "سوهاج", NameEn = "Sohag", DeliveryFee = 20, EtaMinMinutes = 25, EtaMaxMinutes = 35, IsActive = true };

    private static (CreateMenuItemCommandHandler Handler, FakeMenuItemRepository MenuItems, FakeMenuCategoryRepository Categories, FakeBranchRepository Branches)
        BuildHandler()
    {
        var menuItems = new FakeMenuItemRepository();
        var categories = new FakeMenuCategoryRepository();
        var branches = new FakeBranchRepository();
        var handler = new CreateMenuItemCommandHandler(menuItems, categories, branches);
        return (handler, menuItems, categories, branches);
    }

    private static CreateMenuItemCommand ValidCommand(int branchId = 1) => new()
    {
        CategoryKey = "burgers",
        NameAr = "أورجينال",
        NameEn = "Original",
        DescriptionAr = "وصف",
        DescriptionEn = "description",
        Price = 90,
        BranchId = branchId
    };

    [Fact]
    public async Task Handle_creates_menu_item_without_photo_and_returns_documented_shape()
    {
        var (handler, menuItems, categories, branches) = BuildHandler();
        categories.Categories.Add(BurgersCategory());
        branches.Branches.Add(SohagBranch());

        var result = await handler.Handle(ValidCommand(), default);

        Assert.Single(menuItems.MenuItems);
        Assert.Equal("burgers", result.CategoryKey);
        Assert.Equal("أورجينال", result.NameAr);
        Assert.Equal("Original", result.NameEn);
        Assert.Equal(90, result.Price);
        Assert.Null(result.ImageUrl); // §7.6.1: "a new item is always created without a photo"
        Assert.True(result.IsAvailable); // §6.2: default 1
        Assert.Equal(1, menuItems.MenuItems[0].BranchId);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_categoryKey_does_not_exist()
    {
        var (handler, _, _, branches) = BuildHandler();
        branches.Branches.Add(SohagBranch());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(), default));
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_branchId_does_not_exist()
    {
        var (handler, _, categories, _) = BuildHandler();
        categories.Categories.Add(BurgersCategory());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(branchId: 99), default));
    }
}
