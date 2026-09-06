using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;
using RBurger.Infrastructure.Persistence;

namespace RBurger.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone, cancellationToken);
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return _context.Customers.AnyAsync(c => c.Phone == phone, cancellationToken);
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        _context.Customers.Add(customer);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    // ---- Day 11 additions (§7.6.4 Admin Customer Management) ----

    public async Task<(IReadOnlyList<Customer> Customers, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await _context.Customers.CountAsync(cancellationToken);

        var customers = await _context.Customers
            .OrderBy(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (customers, totalCount);
    }

    // §7.6.4: CustomerConfiguration's OnDelete(DeleteBehavior.SetNull) on Customer->Orders
    // (Day 2 approved decision) means the database itself nulls out every referencing
    // Order.CustomerId as part of this same DELETE statement, inside the same
    // SaveChangesAsync transaction - no separate Order-side update is issued here. This is
    // what makes the delete-customer + anonymize-orders operation atomic (single DB
    // round-trip/transaction), per Ahmed's explicit atomicity requirement.
    public Task DeleteAsync(Customer customer, CancellationToken cancellationToken)
    {
        _context.Customers.Remove(customer);
        return Task.CompletedTask;
    }
}
