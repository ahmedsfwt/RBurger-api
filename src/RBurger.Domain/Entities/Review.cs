namespace RBurger.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    // §6.1 navigation property (Order (1) -- (0..1) Review)
    public Order Order { get; set; } = null!;

    // §6.2 documents an explicit CustomerId FK column on Reviews, so a reference
    // navigation is added here even though §6.1's relationship list does not enumerate
    // Customer--Review; see discrepancy report.
    public Customer Customer { get; set; } = null!;
}
