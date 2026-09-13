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

    public Task AddAsync(BuilderOptionGroup group, CancellationToken cancellationToken)
    {
        _context.BuilderOptionGroups.Add(group);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Task<BuilderOptionGroup?> GetByIdWithOptionsAsync(int id, CancellationToken cancellationToken)
    {
        return _context.BuilderOptionGroups
            .Include(g => g.Options)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public void Update(BuilderOptionGroup group)
    {
        _context.BuilderOptionGroups.Update(group);
    }

    public void Delete(BuilderOptionGroup group)
    {
        _context.BuilderOptionGroups.Remove(group);
    }

    public Task RemoveOptionsAsync(ICollection<BuilderOption> options, CancellationToken cancellationToken)
    {
        _context.BuilderOptions.RemoveRange(options);
        return Task.CompletedTask;
    }
}
