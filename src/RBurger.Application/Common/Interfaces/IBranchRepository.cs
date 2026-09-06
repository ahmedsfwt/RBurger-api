using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Interfaces;

// Minimal repository abstraction (Day 4 addition). Needed by CreateOrderCommandHandler to
// verify the Branch exists (§7.8: 404 "branch... doesn't exist") and to source the
// server-trusted DeliveryFee (§6.2 Branches.DeliveryFee) rather than trusting the client.
public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(int id, CancellationToken cancellationToken);

    // ---- Day 10 additions (§7.6.2 Admin Branch Management) ----

    Task AddAsync(Branch branch, CancellationToken cancellationToken);

    // §7.8/§6.3: deletion must be rejected with 422 if the branch still has non-terminal
    // orders or assigned drivers (checked by the handler via IOrderRepository/IDriverRepository
    // before this is ever called). Also guards, as a defensive fallback, against the
    // undocumented edge case where the branch still has MenuItems - Branch.MenuItems'
    // configured FK behavior (BranchConfiguration) is DeleteBehavior.Restrict, a relationship
    // introduced by the Day 2 approved MenuItem.BranchId decision that §7.6.2 never
    // anticipated. See DeleteAsync's implementation comment in BranchRepository for why this
    // lives in Infrastructure (EF-specific DbUpdateException handling) rather than Application.
    Task DeleteAsync(Branch branch, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    // ---- Day 12 addition (§7.6.6 Analytics) ----

    // §7.6.6 GET /api/v1/admin/analytics/orders-by-branch: needs every branch's
    // nameAr/nameEn to pair with GetOrderCountsByBranchAsync's counts, including branches with
    // zero orders (mirrors orders-by-status's example, which lists all 4 stages). Deliberately
    // all branches, not just IsActive ones (§7.3's "list active branches" restriction is scoped
    // to that public customer-facing endpoint, not documented as applying here).
    Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken);

    // ---- Day 15 addition (§7.3 GET /api/v1/branches - public customer-facing endpoint) ----

    // "List active branches" - ordered by Id to match §7.3's response example (Sohag id=1
    // before Girga id=2), the only ordering signal the documentation gives.
    Task<List<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken);
}
