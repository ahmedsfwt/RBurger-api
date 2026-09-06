using RBurger.Application.Admin.Menu.Commands.DeleteMenuItem;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class DeleteMenuItemCommandHandlerTests
{
    [Fact]
    public async Task Handle_deletes_existing_menu_item()
    {
        var menuItems = new FakeMenuItemRepository();
        menuItems.MenuItems.Add(new MenuItem { Id = 106, CategoryId = 1, BranchId = 1, NameAr = "أ", NameEn = "A" });
        var handler = new DeleteMenuItemCommandHandler(menuItems);

        await handler.Handle(new DeleteMenuItemCommand { Id = 106 }, default);

        Assert.Empty(menuItems.MenuItems);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_menu_item_does_not_exist()
    {
        var menuItems = new FakeMenuItemRepository();
        var handler = new DeleteMenuItemCommandHandler(menuItems);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteMenuItemCommand { Id = 999 }, default));
    }
}
