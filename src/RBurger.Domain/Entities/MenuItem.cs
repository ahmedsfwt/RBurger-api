namespace RBurger.Domain.Entities;

public class MenuItem
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageObjectKey { get; set; }
    public DateTime? ImageUploadedAt { get; set; }
    public bool IsAvailable { get; set; }

    // §6.1 navigation properties
    public MenuCategory Category { get; set; } = null!;

    // §6.1 states Branch (1) -- (∞) MenuItem, but §6.2's MenuItems column list has no FK column for it.
    // BranchId added as a Day 2 implementation decision (approved) to resolve the undocumented
    // FK-name gap; see discrepancy report.
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
