using RBurger.Domain.Enums;

namespace RBurger.Domain.Entities;

public class OrderStatusEvent
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }

    // §6.2 lists this column as tinyint ("the stage transitioned to"), the same domain
    // concept as Order.Stage. Per §12.1's rule that the stage enum must never be
    // re-declared locally with different numbers, this reuses the single OrderStage enum
    // rather than a raw byte/tinyint. See discrepancy report.
    public OrderStage Stage { get; set; }

    public string TriggeredBy { get; set; } = string.Empty;

    // §6.2: "DriverId or CustomerId, nullable for system" - polymorphic actor reference,
    // no single-table FK is documented, so no navigation property is added for it.
    public Guid? ActorId { get; set; }

    public DateTime Timestamp { get; set; }

    // §6.1 navigation property
    public Order Order { get; set; } = null!;
}
