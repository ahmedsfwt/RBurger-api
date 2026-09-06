namespace RBurger.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DefaultAddress { get; set; }
    public string PreferredLanguage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // §6.1 navigation property (Customer (1) -- (∞) Order)
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    // Note: §6.2's Reviews table has an explicit CustomerId FK column, but §6.1's
    // relationship summary does not list a Customer--Review relationship. No reverse
    // collection is added here to avoid going beyond the documented §6.1 list;
    // see discrepancy report. The FK itself is represented on Review.Customer.
}
