using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Interfaces;

// Day 10 addition. §7.6.1's Create/Update menu-item request bodies identify a category by its
// documented "categoryKey" string (§6.2 MenuCategories.Key), not by CategoryId - this
// abstraction resolves that lookup without leaking EF Core/DbContext into Application.
public interface IMenuCategoryRepository
{
    Task<MenuCategory?> GetByKeyAsync(string key, CancellationToken cancellationToken);

    // Day 10 addition: needed by UpdateMenuItemCommandHandler to resolve the response's
    // documented "categoryKey" field from MenuItem.CategoryId when the category itself wasn't
    // part of the update request (i.e. it must reflect the item's current, possibly-unchanged
    // category, not just an echo of the request body).
    Task<MenuCategory?> GetByIdAsync(int id, CancellationToken cancellationToken);

    // Day 15 addition (§7.3 GET /api/v1/menu?branchId= - public endpoint): categories ordered
    // by §6.2's SortOrder column, feeding the Menu tabs (§2.4).
    Task<List<MenuCategory>> GetAllOrderedAsync(CancellationToken cancellationToken);
    Task AddAsync(MenuCategory category, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
