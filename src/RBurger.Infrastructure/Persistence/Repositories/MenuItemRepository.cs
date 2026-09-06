using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Repositories;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly ApplicationDbContext _context;

    public MenuItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<MenuItem>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        return _context.MenuItems
            .Where(mi => ids.Contains(mi.Id))
            .ToListAsync(cancellationToken);
    }

    // Day 15 addition (§7.3 - public endpoint).
    public Task<List<MenuItem>> GetAvailableByBranchIdAsync(int branchId, CancellationToken cancellationToken)
    {
        return _context.MenuItems
            .Where(mi => mi.BranchId == branchId && mi.IsAvailable)
            .ToListAsync(cancellationToken);
    }

    // ---- Day 10 additions (§7.6.1 Admin Menu Management) ----

    public Task<MenuItem?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _context.MenuItems.FirstOrDefaultAsync(mi => mi.Id == id, cancellationToken);
    }

    public Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken)
    {
        _context.MenuItems.Add(menuItem);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MenuItem menuItem, CancellationToken cancellationToken)
    {
        _context.MenuItems.Remove(menuItem);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
