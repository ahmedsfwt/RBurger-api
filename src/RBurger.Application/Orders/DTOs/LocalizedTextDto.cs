namespace RBurger.Application.Orders.DTOs;

// §7.4 POST /api/v1/orders request example: customName / customDescription are
// { "ar": "...", "en": "..." } pairs for custom-built burger items (menuItemId: null).
public class LocalizedTextDto
{
    public string Ar { get; set; } = string.Empty;
    public string En { get; set; } = string.Empty;
}
