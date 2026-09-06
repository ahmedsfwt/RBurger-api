namespace RBurger.Domain.Entities;

public class OrderItem
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public int? MenuItemId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // §6.1 navigation properties
    public Order Order { get; set; } = null!;
    public MenuItem? MenuItem { get; set; }
}
