using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _context;

    public AdminRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Admin?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return _context.Admins.FirstOrDefaultAsync(a => a.Username == username, cancellationToken);
    }

    public Task<Admin?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.Admins.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
    public Task AddAsync(Admin admin, CancellationToken cancellationToken)
    {
        _context.Admins.Add(admin);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
