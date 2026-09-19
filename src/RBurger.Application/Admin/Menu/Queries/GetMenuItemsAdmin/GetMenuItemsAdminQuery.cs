using MediatR;
using RBurger.Application.Admin.Menu.DTOs;

namespace RBurger.Application.Admin.Menu.Queries.GetMenuItemsAdmin;

public class GetMenuItemsAdminQuery : IRequest<List<MenuItemAdminResponse>>
{
}