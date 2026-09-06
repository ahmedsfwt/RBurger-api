namespace RBurger.Domain.Entities;

public class Branch
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public int EtaMinMinutes { get; set; }
    public int EtaMaxMinutes { get; set; }
    public string HotlinePhones { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Day 14 addition (Backend Parity Spec §1.4): ETA is explicitly defined as this
    // branch-configured text, not a computed numeric formula. Nullable - existing branches
    // have no value until an Admin sets one via Create/Update.
    public string? EstimatedDeliveryTime { get; set; }

    // §6.1 navigation properties
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
}
