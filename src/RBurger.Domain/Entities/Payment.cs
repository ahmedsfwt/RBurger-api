namespace RBurger.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? GatewayProvider { get; set; }
    public string? GatewayTransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }

    // Added under Ahmed's explicit Day 13 approval to modify the §6.2 schema, resolving the
    // Day 12 blocker for §9.4's captured-cash cancellation branch: "simply records a
    // manual-refund note for cash payments already collected" had no backing column. Nullable -
    // set only when DeleteAdminOrderCommandHandler takes this specific branch.
    public string? CashRefundNote { get; set; }
    public DateTime? CashRefundNotedAt { get; set; }

    // §6.1 navigation property (Order (1) -- (0..1) Payment)
    public Order Order { get; set; } = null!;
}
