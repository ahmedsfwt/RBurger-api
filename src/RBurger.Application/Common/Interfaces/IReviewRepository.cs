using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Interfaces;

// Minimal repository abstraction (Day 5 addition), following the exact same pattern as
// ICustomerRepository (Day 3) / IOrderRepository (Day 4). Keeps Application independent of
// Infrastructure/EF Core.
public interface IReviewRepository
{
    // §6.3: "A Review can only be created once ... only once per order (unique index on
    // OrderId)." Used as the fast-path pre-check in CreateReviewCommandHandler before insert;
    // the DB's unique index (ReviewConfiguration) remains the final safety net for a race
    // between two concurrent requests - see SaveChangesAsync below.
    Task<bool> ExistsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(Review review, CancellationToken cancellationToken);

    // If the pre-check above loses a race (two concurrent requests both pass
    // ExistsByOrderIdAsync before either commits), the DB's unique index on Reviews.OrderId
    // rejects the second insert with a DbUpdateException. Per the Clean Architecture rule
    // already established in Day 4 (EF Core-specific exceptions must not leak into
    // Application), the Infrastructure implementation of this method is responsible for
    // catching that DbUpdateException and translating it into Application's ConflictException.
    Task SaveChangesAsync(CancellationToken cancellationToken);

    // ---- Day 12 additions (§7.6.6 Reviews & Analytics) ----

    // §7.6.6 GET /api/v1/admin/reviews: "paginated" with "order/customer context". Includes
    // Order (for orderNumber/customerName - see AdminReviewListItemDto's comment on why
    // Order.CustomerName is used instead of Customer.FullName). §12.3 Decision 3 (approved):
    // CreatedAt descending default order, matching every other list endpoint.
    Task<(IReadOnlyList<Review> Reviews, int TotalCount)> GetPagedForAdminAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    // §7.6.6 DELETE /api/v1/admin/reviews/{id}.
    Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken);

    Task DeleteAsync(Review review, CancellationToken cancellationToken);

    // §7.6.6 GET /api/v1/admin/analytics/rating-distribution: "Review counts grouped by star
    // rating (1-5)" - no date range documented, all-time.
    Task<Dictionary<byte, int>> GetRatingDistributionAsync(CancellationToken cancellationToken);
}
