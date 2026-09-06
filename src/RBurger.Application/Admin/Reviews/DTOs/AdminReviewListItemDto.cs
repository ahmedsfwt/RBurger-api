namespace RBurger.Application.Admin.Reviews.DTOs;

// §7.6.6 GET /api/v1/admin/reviews list item example:
// { "reviewId","orderNumber","customerName","rating","comment" }.
// customerName is sourced from Order.CustomerName (the §6.2 order-time snapshot) rather than
// Review.Customer.FullName - ReviewConfiguration sets Restrict on Review -> Customer, and using
// the snapshot avoids depending on the live Customer row still existing/being loaded, exactly
// mirroring why AdminOrderListItemDto also reads Order.CustomerName instead of joining Customer.
public class AdminReviewListItemDto
{
    public Guid ReviewId { get; set; }
    public int OrderNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public byte Rating { get; set; }
    public string? Comment { get; set; }
}
