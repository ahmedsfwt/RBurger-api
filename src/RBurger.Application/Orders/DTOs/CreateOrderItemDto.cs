namespace RBurger.Application.Orders.DTOs;

// §7.4 POST /api/v1/orders request "items[]" - two documented shapes:
//   catalog item:  { "menuItemId": 101, "quantity": 2 }
//   custom burger: { "menuItemId": null, "quantity": 1, "customName": {...},
//                    "customDescription": {...}, "unitPrice": 145 }
// Both shapes are modeled as one DTO with nullable fields, matching the documented JSON
// exactly rather than inventing two separate request types.
public class CreateOrderItemDto
{
    public int? MenuItemId { get; set; }
    public int Quantity { get; set; }

    // Only meaningful when MenuItemId is null (custom burger). Server-trusted pricing rule
    // (approved decision #3): a client-supplied UnitPrice is ONLY honored here, for custom
    // items. For catalog items (MenuItemId set), this field is ignored by the handler and
    // MenuItem.Price is used instead.
    public LocalizedTextDto? CustomName { get; set; }
    public LocalizedTextDto? CustomDescription { get; set; }
    public decimal? UnitPrice { get; set; }
}
