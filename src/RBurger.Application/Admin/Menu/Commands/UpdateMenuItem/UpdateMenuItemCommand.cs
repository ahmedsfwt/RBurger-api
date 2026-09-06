using MediatR;
using RBurger.Application.Admin.Menu.DTOs;

namespace RBurger.Application.Admin.Menu.Commands.UpdateMenuItem;

// §7.6.1 PUT /api/v1/admin/menu-items/{id}: "may update: name/description in both languages,
// price, category, availability. Image is deliberately not part of this JSON body." The
// documented request example only sends a subset ({ price, isAvailable }) - modeled here as
// all-optional/nullable so any subset of documented fields can be sent (Day 10 decision,
// consistent with the example being a partial update, not an exhaustive one).
public class UpdateMenuItemCommand : IRequest<MenuItemAdminResponse>
{
    public int Id { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public decimal? Price { get; set; }
    public string? CategoryKey { get; set; }
    public bool? IsAvailable { get; set; }
}
