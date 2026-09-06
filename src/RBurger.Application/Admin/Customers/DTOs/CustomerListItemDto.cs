namespace RBurger.Application.Admin.Customers.DTOs;

// §7.6.4 GET /api/v1/admin/customers list item example: { customerId, fullName, phone,
// ordersCount }.
public class CustomerListItemDto
{
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int OrdersCount { get; set; }
}
