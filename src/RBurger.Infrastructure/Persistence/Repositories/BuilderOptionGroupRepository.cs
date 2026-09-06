using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Repositories;

// Day 15 addition (§7.3 - public endpoint).
public class BuilderOptionGroupRepository : IBuilderOptionGroupRepository
{
    private readonly ApplicationDbContext _context;

    public BuilderOptionGroupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<BuilderOptionGroup>> GetAllWithOptionsAsync(CancellationToken cancellationToken)
    {
        return _context.BuilderOptionGroups
            .Include(g => g.Options)
            .ToListAsync(cancellationToken);
    }
}
