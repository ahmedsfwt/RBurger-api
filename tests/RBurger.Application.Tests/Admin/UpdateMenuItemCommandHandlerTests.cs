using RBurger.Application.Admin.Menu.Commands.UpdateMenuItem;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class UpdateMenuItemCommandHandlerTests
{
    private static MenuCategory BurgersCategory() => new() { Id = 1, Key = "burgers", LabelAr = "البرجر", LabelEn = "Burgers", SortOrder = 1 };
    private static MenuCategory PizzaCategory() => new() { Id = 2, Key = "pizza", LabelAr = "بيتزا", LabelEn = "Pizza", SortOrder = 2 };

    private static MenuItem ExistingItem() => new()
    {
        Id = 106,
        CategoryId = 1,
        BranchId = 1,
        NameAr = "أورجينال",
        NameEn = "Original",
        DescriptionAr = "وصف",
        DescriptionEn = "description",
        Price = 90,
        IsAvailable = true
    };

    private static (UpdateMenuItemCommandHandler Handler, FakeMenuItemRepository MenuItems, FakeMenuCategoryRepository Categories)
        BuildHandler()
    {
        var menuItems = new FakeMenuItemRepository();
        var categories = new FakeMenuCategoryRepository();
        categories.Categories.Add(BurgersCategory());
        categories.Categories.Add(PizzaCategory());
        var handler = new UpdateMenuItemCommandHandler(menuItems, categories);
        return (handler, menuItems, categories);
    }

    // §7.6.1's documented example: request { price, isAvailable } -> only those fields change.
    [Fact]
    public async Task Handle_partially_updates_only_the_sent_fields()
    {
        var (handler, menuItems, _) = BuildHandler();
        var item = ExistingItem();
        menuItems.MenuItems.Add(item);

        var result = await handler.Handle(new UpdateMenuItemCommand { Id = 106, Price = 95, IsAvailable = true }, default);

        Assert.Equal(95, result.Price);
        Assert.True(result.IsAvailable);
        Assert.Equal("أورجينال", result.NameAr); // unchanged
        Assert.Equal("burgers", result.CategoryKey); // unchanged
    }

    [Fact]
    public async Task Handle_updates_category_when_categoryKey_is_sent()
    {
        var (handler, menuItems, _) = BuildHandler();
        menuItems.MenuItems.Add(ExistingItem());

        var result = await handler.Handle(new UpdateMenuItemCommand { Id = 106, CategoryKey = "pizza" }, default);

        Assert.Equal("pizza", result.CategoryKey);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_menu_item_does_not_exist()
    {
        var (handler, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateMenuItemCommand { Id = 999, Price = 10 }, default));
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_new_categoryKey_does_not_exist()
    {
        var (handler, menuItems, _) = BuildHandler();
        menuItems.MenuItems.Add(ExistingItem());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateMenuItemCommand { Id = 106, CategoryKey = "nonexistent" }, default));
    }

    // §6.2/§7.6.1 binding rule: image is never accepted through this endpoint - the command
    // shape itself has no image fields at all (see UpdateMenuItemCommand), so this test simply
    // documents that an update never touches ImageUrl/ImageObjectKey/ImageUploadedAt.
    [Fact]
    public async Task Handle_never_changes_image_fields()
    {
        var (handler, menuItems, _) = BuildHandler();
        var item = ExistingItem();
        item.ImageUrl = "https://media.rburger.app/menu-items/106/existing.jpg";
        item.ImageObjectKey = "menu-items/106/existing.jpg";
        item.ImageUploadedAt = DateTime.UtcNow;
        menuItems.MenuItems.Add(item);

        var result = await handler.Handle(new UpdateMenuItemCommand { Id = 106, Price = 100 }, default);

        Assert.Equal("https://media.rburger.app/menu-items/106/existing.jpg", result.ImageUrl);
    }
}
