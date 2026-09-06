using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Interfaces;

// Minimal repository abstraction (Day 3 addition, not part of the documented API surface).
// Needed to keep Application independent of Infrastructure/EF Core per the architecture rules,
// without leaking DbSet<T>/EF Core types into this layer. Flagged in the Day 3 report.
public interface ICustomerRepository
{
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken);
    Task AddAsync(Customer customer, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    // ---- Day 11 additions (§7.6.4 Admin Customer Management) ----

    // §7.6.4 GET /api/v1/admin/customers - paginated per §7.0's convention, mirroring
    // IDriverRepository.GetPagedAsync's shape (Day 11).
    Task<(IReadOnlyList<Customer> Customers, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    // §7.6.4 DELETE /api/v1/admin/customers/{id}: "Delete a customer account ... Order history
    // rows are retained ... but CustomerId is nulled." CustomerConfiguration (Day 2 approved
    // decision) already configures Customer->Orders as OnDelete(DeleteBehavior.SetNull), so a
    // plain EF Core Remove()+SaveChangesAsync is sufficient for the DB to null out every
    // referencing Order.CustomerId atomically as part of the same DELETE statement - no manual
    // Order-side update is required in Infrastructure's real implementation (see
    // CustomerRepository.DeleteAsync's XML comment for the in-memory fake's necessary
    // divergence, since fakes cannot simulate a DB-level ON DELETE SET NULL constraint).
    Task DeleteAsync(Customer customer, CancellationToken cancellationToken);
}
