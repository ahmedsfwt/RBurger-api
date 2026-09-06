using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Repositories;

public class MenuCategoryRepository : IMenuCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public MenuCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<MenuCategory?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return _context.MenuCategories.FirstOrDefaultAsync(mc => mc.Key == key, cancellationToken);
    }

    public Task<MenuCategory?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _context.MenuCategories.FirstOrDefaultAsync(mc => mc.Id == id, cancellationToken);
    }

    // Day 15 addition (§7.3 - public endpoint).
    public Task<List<MenuCategory>> GetAllOrderedAsync(CancellationToken cancellationToken)
    {
        return _context.MenuCategories.OrderBy(mc => mc.SortOrder).ToListAsync(cancellationToken);
    }
}
