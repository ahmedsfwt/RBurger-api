using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Menu.Commands.DeleteMenuItem;

public class DeleteMenuItemCommandHandler : IRequestHandler<DeleteMenuItemCommand>
{
    private readonly IMenuItemRepository _menuItemRepository;

    public DeleteMenuItemCommandHandler(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public async Task Handle(DeleteMenuItemCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "menu item ... doesn't exist".
        var menuItem = await _menuItemRepository.GetByIdAsync(request.Id, cancellationToken);
        if (menuItem is null)
        {
            throw new NotFoundException($"Menu item {request.Id} was not found.");
        }

        // §7.6.1: "Permanently delete a menu item (and its S3 photo object, if any, §10.1).
        // Existing OrderItems keep their NameAr/NameEn/UnitPrice snapshot ... past orders are
        // unaffected." MenuItemConfiguration's OnDelete(SetNull) on OrderItem.MenuItemId
        // already guarantees the snapshot survives - no extra handling needed here for that
        // part of the rule.
        //
        // The "(and its S3 photo object, if any)" part is intentionally NOT executed here:
        // per the Day 10 approved scaffold-only decision, no real image storage exists, so
        // ImageObjectKey can never actually be non-null for a real MenuItem in this
        // environment (the upload endpoint always throws StorageNotConfiguredException - see
        // NotConfiguredMenuItemImageStorage). Calling IMenuItemImageStorage.DeleteAsync here
        // would either be dead code or would itself throw and block an otherwise-valid
        // deletion. This is deferred to whenever a real storage implementation ships.
        await _menuItemRepository.DeleteAsync(menuItem, cancellationToken);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);
    }
}
