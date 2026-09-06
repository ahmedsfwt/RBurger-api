using MediatR;
using RBurger.Application.Admin.Menu.DTOs;

namespace RBurger.Application.Admin.Menu.Commands.CreateMenuItem;

// §7.6.1 POST /api/v1/admin/menu-items request body: { categoryKey, nameAr, nameEn,
// descriptionAr, descriptionEn, price }.
//
// Day 10 approved decision (Blocking Issue #1): BranchId is added here as a REQUIRED field
// NOT present in Documentation v1.2's §7.6.1 request example. This is a documented-contract
// discrepancy, not a literal part of the spec - it exists because the Day 2 approved Domain
// decision made MenuItem.BranchId a required, non-nullable FK (Restrict delete), and
// CreateOrderCommandHandler already depends on every MenuItem having one (§7.4's
// MENU_ITEM_BRANCH_MISMATCH rule). Approved explicitly by Ahmed ahead of Day 10 coding; no
// fallback/default branch selection is implemented - the field is mandatory.
public class CreateMenuItemCommand : IRequest<MenuItemAdminResponse>
{
    public string CategoryKey { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Not part of the documented §7.6.1 JSON body - see the class-level comment above.
    public int BranchId { get; set; }
}
