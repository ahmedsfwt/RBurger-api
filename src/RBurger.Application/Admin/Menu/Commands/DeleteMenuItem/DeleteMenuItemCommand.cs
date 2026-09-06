using MediatR;

namespace RBurger.Application.Admin.Menu.Commands.DeleteMenuItem;

// §7.6.1 DELETE /api/v1/admin/menu-items/{id} - no request body, 200/204-style no-content
// response (no documented response example, unlike Branch/Driver deletes which also have none).
public class DeleteMenuItemCommand : IRequest
{
    public int Id { get; set; }
}
