using MediatR;
using RBurger.Application.Admin.Menu.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Menu.Commands.ToggleMenuItemAvailability;

public class ToggleMenuItemAvailabilityCommandHandler
    : IRequestHandler<ToggleMenuItemAvailabilityCommand, MenuItemAvailabilityResponse>
{
    private readonly IMenuItemRepository _menuItemRepository;

    public ToggleMenuItemAvailabilityCommandHandler(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public async Task<MenuItemAvailabilityResponse> Handle(
        ToggleMenuItemAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(request.Id, cancellationToken);
        if (menuItem is null)
        {
            throw new NotFoundException($"Menu item {request.Id} was not found.");
        }

        menuItem.IsAvailable = !menuItem.IsAvailable;
        await _menuItemRepository.SaveChangesAsync(cancellationToken);

        return new MenuItemAvailabilityResponse
        {
            Id = menuItem.Id,
            IsAvailable = menuItem.IsAvailable
        };
    }
}
