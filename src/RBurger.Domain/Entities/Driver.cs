namespace RBurger.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Vehicle { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public Guid CreatedByAdminId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // §6.1 navigation properties
    public Branch Branch { get; set; } = null!;
    public Admin CreatedByAdmin { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
