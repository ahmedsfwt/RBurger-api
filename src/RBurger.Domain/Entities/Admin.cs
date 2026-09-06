namespace RBurger.Domain.Entities;

public class Admin
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // §6.1 navigation property (Admin (1) -- (∞) Driver)
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
}
