using RBurger.Domain.Enums;

namespace RBurger.Application.Orders.DTOs;

// §7.4 GET /api/v1/orders/{orderId} response:
// { orderId, orderNumber, stage, branch{...}, items[...], notes, subtotal, deliveryFee,
//   total, payment{...}, customerReceivedAt, review, etaSecondsRemaining }
public class OrderDetailResponse
{
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public OrderStage Stage { get; set; }
    public BranchSummaryDto Branch { get; set; } = new();
    public List<OrderItemSummaryDto> Items { get; set; } = new();
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public PaymentSummaryDto Payment { get; set; } = new();
    public DateTime? CustomerReceivedAt { get; set; }

    // Day 8 approved decision #2: reuses CreateReviewResponse verbatim (same
    // reviewId/orderId/rating/comment/createdAt shape as POST .../review's response) rather
    // than a second, shortened DTO - per §12.3's rule that a field is never re-declared with
    // a different shape between two endpoints that expose the same underlying Review entity.
    // Null when the order has no review yet (unchanged Day 4/5 behavior).
    public CreateReviewResponse? Review { get; set; }

    // Approved decision #6: no documented computation rule exists for this field anywhere
    // in the spec (Branch ETA range, driver assignment, Hangfire timers are all mentioned
    // elsewhere but none is tied to this field). Left null with this explicit note rather
    // than inventing a formula. Day 15/16 (Backend Parity Spec §7): confirmed this remains a
    // genuine, undefined business-rule gap - Branch.EstimatedDeliveryTime (§1.4/Day 14) is
    // free text and deliberately not converted into a numeric seconds value for this field,
    // since no documented formula ties the two together. Still null by design, not an
    // oversight.
    public int? EtaSecondsRemaining { get; set; }
}
