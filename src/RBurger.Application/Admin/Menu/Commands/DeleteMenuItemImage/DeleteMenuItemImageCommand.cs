using MediatR;
using RBurger.Application.Admin.Menu.DTOs;

namespace RBurger.Application.Admin.Menu.Commands.DeleteMenuItemImage;

// §7.6.1 DELETE /api/v1/admin/menu-items/{id}/image.
public class DeleteMenuItemImageCommand : IRequest<MenuItemImageDeleteResponse>
{
    public int MenuItemId { get; set; }
}
