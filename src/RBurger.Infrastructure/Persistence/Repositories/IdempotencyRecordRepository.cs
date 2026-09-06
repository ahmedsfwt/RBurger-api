using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Repositories;

// Day 13 addition (approved schema change) - implements IIdempotencyRepository against
// ApplicationDbContext. The DB's unique index (Endpoint, ScopeId, Key) - see
// IdempotencyRecordConfiguration - is the actual concurrency safety net: TryBeginAsync always
// attempts the INSERT and reports false on a unique-constraint violation, mirroring
// OrderRepository.AddWithGeneratedOrderNumberAsync's catch-DbUpdateException-and-report-false
// pattern. This is why the source of truth is the database, not any in-memory structure.
public class IdempotencyRecordRepository : IIdempotencyRepository
{
    private readonly ApplicationDbContext _context;

    public IdempotencyRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryBeginAsync(IdempotencyRecord record, CancellationToken cancellationToken)
    {
        _context.IdempotencyRecords.Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique index rejected a concurrent duplicate (Endpoint, ScopeId, Key) - detach
            // the failed entry so this DbContext instance stays usable for the rest of the
            // request (the DbContext is scoped per-HTTP-request in this codebase).
            _context.Entry(record).State = EntityState.Detached;
            return false;
        }
    }

    public Task<IdempotencyRecord?> FindAsync(
        string endpoint, Guid scopeId, string key, CancellationToken cancellationToken)
    {
        return _context.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Endpoint == endpoint && r.ScopeId == scopeId && r.Key == key,
                cancellationToken);
    }

    public async Task CompleteAsync(Guid id, string responseJson, CancellationToken cancellationToken)
    {
        var rowsAffected = await _context.IdempotencyRecords
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, "Completed")
                    .SetProperty(r => r.ResponseJson, responseJson)
                    .SetProperty(r => r.CompletedAt, DateTime.UtcNow),
                cancellationToken);

        _ = rowsAffected; // no-op if the row was somehow already removed - nothing to complete
    }

    public async Task AbandonAsync(Guid id, CancellationToken cancellationToken)
    {
        await _context.IdempotencyRecords
            .Where(r => r.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
