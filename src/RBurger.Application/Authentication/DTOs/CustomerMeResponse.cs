namespace RBurger.Application.Authentication.DTOs;

// §7.1 - GET /api/v1/customers/me response shape.
public class CustomerMeResponse
{
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
}
