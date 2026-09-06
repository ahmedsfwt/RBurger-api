using RBurger.Domain.Enums;

namespace RBurger.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public int OrderNumber { get; set; }
    public int BranchId { get; set; }

    // §6.2 documents this as a plain required FK, but §7.6.4 explicitly states CustomerId is
    // nulled when a customer account is deleted (GDPR-style erasure). Changed to nullable as
    // an approved Day 2 decision to resolve this documentation contradiction; see discrepancy report.
    public Guid? CustomerId { get; set; }
    public Guid? DriverId { get; set; }
    public OrderStage Stage { get; set; }

    // §6.2: "CustomerName / Phone / Address" - snapshot at order time, not FK'd live to profile
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime? CustomerReceivedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Added under Ahmed's explicit Day 13 approval to modify the §6.2 schema, resolving the
    // Day 12 blocker recorded in OrderCancellationNotDocumentedException/the Day 12 report:
    // §9.4 describes an admin-cancellation outcome ("marked cancelled with no money movement"
    // for pending/authorized payments, and the cash manual-refund-note case) with no backing
    // Stage value or flag column. Order.Stage (§1.3/§6.2/§12.1) remains the closed, binding
    // 0-3 lifecycle enum and is NEVER repurposed or extended with a 4th value - cancellation
    // is layered on top as an orthogonal flag, exactly like CustomerReceivedAt is layered on
    // top of Stage=Delivered rather than becoming a 5th Stage value.
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }

    // §6.1 navigation properties
    public Branch Branch { get; set; } = null!;

    // Nullable to match the now-nullable CustomerId FK (§7.6.4 - see comment above).
    public Customer? Customer { get; set; }
    public Driver? Driver { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusEvent> OrderStatusEvents { get; set; } = new List<OrderStatusEvent>();
    public Payment? Payment { get; set; }
    public Review? Review { get; set; }
}
