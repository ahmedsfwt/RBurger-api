namespace RBurger.Application.Orders.DTOs;

// §7.4 POST /api/v1/orders/{orderId}/review response:
// { "reviewId":"a71b...", "orderId":"9c41...", "rating":5, "comment":"...", "createdAt":"..." }
public class CreateReviewResponse
{
    public Guid ReviewId { get; set; }
    public Guid OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
