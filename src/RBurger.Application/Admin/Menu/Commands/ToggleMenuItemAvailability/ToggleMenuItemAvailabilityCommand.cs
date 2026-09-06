using MediatR;
using RBurger.Application.Admin.Menu.DTOs;

namespace RBurger.Application.Admin.Menu.Commands.ToggleMenuItemAvailability;

// Day 14 addition (Backend Parity Spec §2.2). Not documented in v1.2 - §7.6.1 only exposes
// IsAvailable via PUT /api/v1/admin/menu-items/{id}'s isAvailable field. This is a new,
// additional route (does not replace or duplicate the PUT contract): flips the item's current
// IsAvailable value with no request body, for a one-tap toggle UX.
public record ToggleMenuItemAvailabilityCommand(int Id) : IRequest<MenuItemAvailabilityResponse>;
