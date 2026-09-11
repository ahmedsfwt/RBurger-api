using MediatR;
using RBurger.Application.Admin.Menu.DTOs;

namespace RBurger.Application.Admin.Menu.Commands.CreateMenuCategory;

public class CreateMenuCategoryCommand : IRequest<MenuCategoryAdminResponse>
{
    public string CategoryKey { get; set; } = string.Empty;
    public string LabelAr { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}