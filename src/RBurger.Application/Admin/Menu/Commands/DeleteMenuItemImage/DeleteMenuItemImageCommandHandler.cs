using MediatR;
using RBurger.Application.Admin.Menu.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Menu.Commands.DeleteMenuItemImage;

public class DeleteMenuItemImageCommandHandler
    : IRequestHandler<DeleteMenuItemImageCommand, MenuItemImageDeleteResponse>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemImageStorage _imageStorage;

    public DeleteMenuItemImageCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuItemImageStorage imageStorage)
    {
        _menuItemRepository = menuItemRepository;
        _imageStorage = imageStorage;
    }

    public async Task<MenuItemImageDeleteResponse> Handle(
        DeleteMenuItemImageCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "menu item ... doesn't exist".
        var menuItem = await _menuItemRepository.GetByIdAsync(request.MenuItemId, cancellationToken);
        if (menuItem is null)
        {
            throw new NotFoundException($"Menu item {request.MenuItemId} was not found.");
        }

        // §7.6.1 doesn't document behavior for deleting a photo that doesn't exist. Treated
        // as idempotent here (no-op, return imageUrl: null) rather than throwing - this also
        // means the scaffold-only storage (which always throws StorageNotConfiguredException,
        // see NotConfiguredMenuItemImageStorage) is never invoked for the realistic Day 10
        // case where no MenuItem can actually have an image yet (upload is non-functional).
        if (menuItem.ImageObjectKey is not null)
        {
            await _imageStorage.DeleteAsync(menuItem.ImageObjectKey, cancellationToken);

            menuItem.ImageUrl = null;
            menuItem.ImageObjectKey = null;
            menuItem.ImageUploadedAt = null;

            await _menuItemRepository.SaveChangesAsync(cancellationToken);
        }

        return new MenuItemImageDeleteResponse
        {
            Id = menuItem.Id,
            ImageUrl = menuItem.ImageUrl
        };
    }
}
