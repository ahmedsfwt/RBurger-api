using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Interfaces;

// Minimal repository abstraction (Day 4 addition). Needed by CreateOrderCommandHandler to
// re-price catalog items server-side (approved decision #3), check IsAvailable (approved
// decision #4), and check BranchId (approved decision #5) - all requiring a DB read the
// client's request body cannot be trusted for.
public interface IMenuItemRepository
{
    Task<List<MenuItem>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken);

    // Day 15 addition (§7.3 GET /api/v1/menu?branchId= - public endpoint). Filters to
    // IsAvailable items only: the documented purpose of this bit column (toggled via
    // §7.6.1/Day 14's toggle-availability endpoint) is precisely "can currently be ordered by
    // a customer" - an item hidden by an Admin must not still appear on the public menu.
    Task<List<MenuItem>> GetAvailableByBranchIdAsync(int branchId, CancellationToken cancellationToken);

    // ---- Day 10 additions (§7.6.1 Admin Menu Management) ----

    // Single-item lookup for update/delete/image-upload handlers - distinct from
    // GetByIdsAsync's bulk shape (Day 4) which is only used by order pricing.
    Task<MenuItem?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken);

    Task DeleteAsync(MenuItem menuItem, CancellationToken cancellationToken);

    // Explicit persist for both AddAsync and in-place mutations (update/image fields),
    // following the same explicit Add/SaveChanges split already established by
    // ICustomerRepository (Day 3) and IReviewRepository (Day 5).
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
