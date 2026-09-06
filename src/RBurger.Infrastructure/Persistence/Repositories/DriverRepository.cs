using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly ApplicationDbContext _context;

    public DriverRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Driver?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return _context.Drivers.FirstOrDefaultAsync(d => d.Phone == phone, cancellationToken);
    }

    // Day 7 addition - see IDriverRepository's XML comment.
    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.Drivers.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    // Day 10 addition (§7.6.2 Branch deletion rule) - see IDriverRepository's XML comment for
    // the "any Driver row FK'd to this branch, regardless of IsActive" interpretation.
    public Task<bool> HasDriversForBranchAsync(int branchId, CancellationToken cancellationToken)
    {
        return _context.Drivers.AnyAsync(d => d.BranchId == branchId, cancellationToken);
    }

    // ---- Day 11 additions (§7.6.3 Admin Driver Management) ----

    public Task AddAsync(Driver driver, CancellationToken cancellationToken)
    {
        _context.Drivers.Add(driver);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<Driver> Drivers, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await _context.Drivers.CountAsync(cancellationToken);

        var drivers = await _context.Drivers
            .OrderBy(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (drivers, totalCount);
    }

    public Task DeleteAsync(Driver driver, CancellationToken cancellationToken)
    {
        _context.Drivers.Remove(driver);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Defensive fallback, same pattern/rationale as BranchRepository.SaveChangesAsync's
            // Day 10 comment: DriverConfiguration configures Driver->Orders as
            // OnDelete(DeleteBehavior.Restrict) (approved decision #10 - no documented delete
            // behavior for a driver's historical orders), so a driver that has ANY order at all
            // referencing it - including a terminal (Delivered) one that already passed
            // DeleteDriverCommandHandler's §7.6.3 "non-terminal orders" 422 check - still fails
            // this physical DELETE with a FK constraint violation. §7.6.3 only documents the
            // non-terminal-orders 422 case; this FK-Restrict edge case for terminal/historical
            // orders is an undocumented consequence of the Day 6 schema decision, not literally
            // itemized in §7.6.3 or §7.8. Mapped to 422 (not an unhandled 500) with a distinct
            // error code so it is honestly distinguishable from the documented rule if the
            // Admin Dashboard needs to show a different message - see Day 11 report's
            // discrepancy section.
            throw new UnprocessableEntityException(
                "Driver cannot be deleted because it still has associated order history.",
                "DRIVER_HAS_ORDER_HISTORY");
        }
    }

    // ---- Day 12 addition (§7.6.6 Analytics) ----

    public Task<List<Driver>> GetAllActiveAsync(CancellationToken cancellationToken)
    {
        return _context.Drivers.Where(d => d.IsActive).ToListAsync(cancellationToken);
    }
}
