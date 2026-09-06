using Microsoft.EntityFrameworkCore;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Persistence.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return _context.Reviews.AnyAsync(r => r.OrderId == orderId, cancellationToken);
    }

    public Task AddAsync(Review review, CancellationToken cancellationToken)
    {
        _context.Reviews.Add(review);
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
            // §6.3 unique index on Reviews.OrderId (ReviewConfiguration) rejected a concurrent
            // duplicate that slipped past CreateReviewCommandHandler's ExistsByOrderIdAsync
            // pre-check. §7.8: 409 Conflict, "duplicate review" (literal). This is the only
            // place an EF Core exception is caught/translated - CreateReviewCommandHandler
            // never sees DbUpdateException, per the Clean Architecture rule already
            // established in Day 4 for OrderRepository's OrderNumber retry.
            throw new ConflictException("A review already exists for this order.", "DUPLICATE_REVIEW");
        }
    }

    // ---- Day 12 additions (§7.6.6 Reviews & Analytics) ----

    public async Task<(IReadOnlyList<Review> Reviews, int TotalCount)> GetPagedForAdminAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Reviews.Include(r => r.Order).AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (reviews, totalCount);
    }

    public Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken)
    {
        return _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
    }

    public Task DeleteAsync(Review review, CancellationToken cancellationToken)
    {
        _context.Reviews.Remove(review);
        return Task.CompletedTask;
    }

    // Day 14: cancelled orders excluded (Backend Parity Spec §1.3) - a review can only exist
    // once Stage=Delivered/CustomerReceivedAt is set (§6.3), so this is a defensive edge-case
    // guard rather than a common scenario, applied for consistency with every other analytics
    // aggregation.
    public async Task<Dictionary<byte, int>> GetRatingDistributionAsync(
        CancellationToken cancellationToken)
    {
        var counts = await _context.Reviews
            .Where(r => !r.Order.IsCancelled)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Rating, x => x.Count);
    }
}
