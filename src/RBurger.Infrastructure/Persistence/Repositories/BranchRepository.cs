using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly ApplicationDbContext _context;

    public BranchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Branch?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    // ---- Day 10 additions (§7.6.2 Admin Branch Management) ----

    public Task AddAsync(Branch branch, CancellationToken cancellationToken)
    {
        _context.Branches.Add(branch);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Branch branch, CancellationToken cancellationToken)
    {
        _context.Branches.Remove(branch);
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
            // Defensive fallback (Infrastructure's job per the "keep EF-specific exceptions out
            // of Application" rule): §7.6.2 only documents the 422 rule for non-terminal
            // orders/assigned drivers, both already checked by DeleteBranchCommandHandler
            // before this call. It does NOT anticipate the undocumented edge case introduced
            // by the Day 2 approved MenuItem.BranchId decision - BranchConfiguration's
            // OnDelete(DeleteBehavior.Restrict) on Branch->MenuItems means SaveChangesAsync
            // throws DbUpdateException here if the branch still has any MenuItems (a
            // dependency §7.6.2 never anticipated), which would otherwise surface as an
            // unhandled 500. This repository's SaveChangesAsync is only ever invoked from
            // Create/Update/Delete Branch flows (no unique-constraint races expected on
            // Branch, unlike Orders' OrderNumber), so any DbUpdateException reaching here is
            // mapped to the same 422 family instead of leaking as a 500.
            throw new UnprocessableEntityException(
                "Branch cannot be deleted because it still has associated data (e.g. menu items).",
                "BRANCH_HAS_DEPENDENCIES");
        }
    }

    // ---- Day 12 addition (§7.6.6 Analytics) ----

    public Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _context.Branches.ToListAsync(cancellationToken);
    }

    // Day 15 addition (§7.3 - public endpoint).
    public Task<List<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken)
    {
        return _context.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);
    }
}
