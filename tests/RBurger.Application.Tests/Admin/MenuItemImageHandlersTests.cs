using RBurger.Application.Admin.Menu.Commands.DeleteMenuItemImage;
using RBurger.Application.Admin.Menu.Commands.UploadMenuItemImage;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class MenuItemImageHandlersTests
{
    private static MenuItem ExistingItem(string? imageObjectKey = null) => new()
    {
        Id = 106,
        CategoryId = 1,
        BranchId = 1,
        NameAr = "أ",
        NameEn = "A",
        ImageObjectKey = imageObjectKey,
        ImageUrl = imageObjectKey is null ? null : $"https://media.rburger.app/{imageObjectKey}"
    };

    // ---- Upload ----

    [Fact]
    public async Task Upload_throws_NotFoundException_when_menu_item_does_not_exist()
    {
        var menuItems = new FakeMenuItemRepository();
        var storage = new FakeMenuItemImageStorage();
        var handler = new UploadMenuItemImageCommandHandler(menuItems, storage);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UploadMenuItemImageCommand { MenuItemId = 999 }, default));
    }

    // Day 10 approved scaffold-only decision: with no real S3/AWS provider configured, the
    // upload endpoint must surface a clear, honest failure rather than silently "succeeding".
    [Fact]
    public async Task Upload_throws_StorageNotConfiguredException_when_storage_is_not_configured()
    {
        var menuItems = new FakeMenuItemRepository();
        menuItems.MenuItems.Add(ExistingItem());
        var storage = new FakeMenuItemImageStorage { AlwaysThrow = true };
        var handler = new UploadMenuItemImageCommandHandler(menuItems, storage);

        await Assert.ThrowsAsync<StorageNotConfiguredException>(() =>
            handler.Handle(new UploadMenuItemImageCommand { MenuItemId = 106, ContentType = "image/jpeg" }, default));
    }

    // Exercises the orchestration (lookup, field updates, SaveChanges) against a working fake
    // storage, so the handler's own logic is verified independently of Infrastructure's
    // deliberately-unconfigured state.
    [Fact]
    public async Task Upload_updates_ImageUrl_ImageObjectKey_ImageUploadedAt_when_storage_succeeds()
    {
        var menuItems = new FakeMenuItemRepository();
        menuItems.MenuItems.Add(ExistingItem());
        var storage = new FakeMenuItemImageStorage { AlwaysThrow = false };
        var handler = new UploadMenuItemImageCommandHandler(menuItems, storage);

        var result = await handler.Handle(
            new UploadMenuItemImageCommand { MenuItemId = 106, ContentType = "image/jpeg" }, default);

        Assert.Equal(106, result.Id);
        Assert.False(string.IsNullOrEmpty(result.ImageUrl));
        Assert.True(result.ImageUploadedAt > DateTime.MinValue);
        Assert.Equal(result.ImageUrl, menuItems.MenuItems[0].ImageUrl);
    }

    // §7.6.1: "deletes the previous object if one existed" on replacement.
    [Fact]
    public async Task Upload_passes_existing_object_key_to_storage_for_replacement()
    {
        var menuItems = new FakeMenuItemRepository();
        menuItems.MenuItems.Add(ExistingItem("menu-items/106/old.jpg"));
        var storage = new FakeMenuItemImageStorage { AlwaysThrow = false };
        var handler = new UploadMenuItemImageCommandHandler(menuItems, storage);

        await handler.Handle(new UploadMenuItemImageCommand { MenuItemId = 106, ContentType = "image/jpeg" }, default);

        Assert.Contains("menu-items/106/old.jpg", storage.DeletedObjectKeys);
    }

    // ---- Delete ----

    [Fact]
    public async Task DeleteImage_throws_NotFoundException_when_menu_item_does_not_exist()
    {
        var menuItems = new FakeMenuItemRepository();
        var storage = new FakeMenuItemImageStorage();
        var handler = new DeleteMenuItemImageCommandHandler(menuItems, storage);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteMenuItemImageCommand { MenuItemId = 999 }, default));
    }

    // Undocumented edge case (no image to delete) treated as an idempotent no-op - see the
    // handler's XML comment. Crucially, this must NOT invoke the (deliberately) unconfigured
    // storage, since no real MenuItem can have an image yet in this environment.
    [Fact]
    public async Task DeleteImage_is_a_noop_and_returns_null_imageUrl_when_no_image_exists()
    {
        var menuItems = new FakeMenuItemRepository();
        menuItems.MenuItems.Add(ExistingItem());
        var storage = new FakeMenuItemImageStorage { AlwaysThrow = true }; // must not be called
        var handler = new DeleteMenuItemImageCommandHandler(menuItems, storage);

        var result = await handler.Handle(new DeleteMenuItemImageCommand { MenuItemId = 106 }, default);

        Assert.Null(result.ImageUrl);
    }

    [Fact]
    public async Task DeleteImage_clears_fields_and_calls_storage_when_image_exists()
    {
        var menuItems = new FakeMenuItemRepository();
        menuItems.MenuItems.Add(ExistingItem("menu-items/106/existing.jpg"));
        var storage = new FakeMenuItemImageStorage { AlwaysThrow = false };
        var handler = new DeleteMenuItemImageCommandHandler(menuItems, storage);

        var result = await handler.Handle(new DeleteMenuItemImageCommand { MenuItemId = 106 }, default);

        Assert.Null(result.ImageUrl);
        Assert.Null(menuItems.MenuItems[0].ImageObjectKey);
        Assert.Contains("menu-items/106/existing.jpg", storage.DeletedObjectKeys);
    }
}
