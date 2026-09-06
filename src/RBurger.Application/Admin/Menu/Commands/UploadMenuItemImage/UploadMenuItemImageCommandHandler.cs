using MediatR;
using RBurger.Application.Admin.Menu.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Menu.Commands.UploadMenuItemImage;

public class UploadMenuItemImageCommandHandler
    : IRequestHandler<UploadMenuItemImageCommand, MenuItemImageUploadResponse>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemImageStorage _imageStorage;

    public UploadMenuItemImageCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuItemImageStorage imageStorage)
    {
        _menuItemRepository = menuItemRepository;
        _imageStorage = imageStorage;
    }

    public async Task<MenuItemImageUploadResponse> Handle(
        UploadMenuItemImageCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "menu item ... doesn't exist".
        var menuItem = await _menuItemRepository.GetByIdAsync(request.MenuItemId, cancellationToken);
        if (menuItem is null)
        {
            throw new NotFoundException($"Menu item {request.MenuItemId} was not found.");
        }

        // §7.6.1: "deletes the previous object if one existed" - the storage implementation
        // is handed the existing ImageObjectKey (if any) so it can clean it up after a
        // successful replacement. Day 10 scaffold: NotConfiguredMenuItemImageStorage throws
        // StorageNotConfiguredException before any DB mutation happens below, so this call is
        // expected to fail until a real provider is implemented - see that class's XML comment.
        var result = await _imageStorage.UploadAsync(
            menuItem.Id,
            request.Content,
            request.ContentType,
            menuItem.ImageObjectKey,
            cancellationToken);

        menuItem.ImageUrl = result.ImageUrl;
        menuItem.ImageObjectKey = result.ImageObjectKey;
        menuItem.ImageUploadedAt = DateTime.UtcNow;

        await _menuItemRepository.SaveChangesAsync(cancellationToken);

        return new MenuItemImageUploadResponse
        {
            Id = menuItem.Id,
            ImageUrl = menuItem.ImageUrl,
            ImageUploadedAt = menuItem.ImageUploadedAt.Value
        };
    }
}
