using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Interfaces;

// Minimal repository abstraction (Day 6 addition), mirroring ICustomerRepository's read-only
// shape. Day 6 scope is login-only (§7.2), so only the lookup needed by DriverLoginCommandHandler
// is exposed here - no AddAsync/SaveChangesAsync, since driver creation (§7.6.3, POST
// /api/v1/admin/drivers) is out of scope and belongs to the Admin partition, not this day.
public interface IDriverRepository
{
    Task<Driver?> GetByPhoneAsync(string phone, CancellationToken cancellationToken);

    // Day 7 addition: needed by GetDriverNewOrdersQueryHandler to resolve the calling
    // driver's BranchId (§7.5's "own branch" filter) - the Driver JWT carries no branchId
    // claim (approved Day 6 decision), so this must be a server-side lookup by DriverId.
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Day 10 addition (§7.6.2 Branch deletion rule): "Rejected with 422 if ... the branch has
    // assigned drivers." Interpreted as "any Driver row FK'd to this branch" (Driver.BranchId),
    // regardless of IsActive - flagged as an implementation interpretation, approved by Ahmed
    // ahead of Day 10 coding, since §7.6.2 does not further qualify "assigned".
    Task<bool> HasDriversForBranchAsync(int branchId, CancellationToken cancellationToken);

    // ---- Day 11 additions (§7.6.3 Admin Driver Management) ----

    // §7.6.3 POST /api/v1/admin/drivers - persists a new Driver row. This is the single write
    // path approved by §6.3 ("A Drivers row can only be inserted by the command handler behind
    // POST /api/v1/admin/drivers"), following the exact same explicit AddAsync/SaveChangesAsync
    // split already established by ICustomerRepository (Day 3).
    Task AddAsync(Driver driver, CancellationToken cancellationToken);

    // §7.6.3 GET /api/v1/admin/drivers - paginated per §7.0's convention (page/pageSize/
    // totalCount), mirroring IOrderRepository.GetCustomerOrdersPagedAsync's shape.
    Task<(IReadOnlyList<Driver> Drivers, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    // §7.6.3 DELETE /api/v1/admin/drivers/{id}.
    Task DeleteAsync(Driver driver, CancellationToken cancellationToken);

    // §7.6.3 PUT/PATCH/DELETE all need an explicit persist step for an already-tracked Driver
    // mutated in place, following the same pattern as IOrderRepository.SaveChangesAsync (Day 5).
    Task SaveChangesAsync(CancellationToken cancellationToken);

    // ---- Day 12 addition (§7.6.6 Analytics) ----

    // §7.6.6 GET /api/v1/admin/analytics/driver-performance: "Completed-delivery counts per
    // active driver" - the literal documented scope is IsActive drivers only (unlike
    // GetPagedAsync above, which the Drivers tab uses for every driver regardless of status).
    Task<List<Driver>> GetAllActiveAsync(CancellationToken cancellationToken);
}
