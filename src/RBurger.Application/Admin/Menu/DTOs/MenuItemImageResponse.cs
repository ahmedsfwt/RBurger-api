namespace RBurger.Application.Admin.Menu.DTOs;

// §7.6.1 POST .../image response: { id, imageUrl, imageUploadedAt }.
public class MenuItemImageUploadResponse
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime ImageUploadedAt { get; set; }
}

// §7.6.1 DELETE .../image response: { id, imageUrl: null }.
public class MenuItemImageDeleteResponse
{
    public int Id { get; set; }
    public string? ImageUrl { get; set; }
}

// Day 14 addition (Backend Parity Spec §2.2) - narrow response for the new
// PATCH .../menu-items/{id}/toggle-availability route, mirroring DriverStatusResponse's
// existing narrow-scoped precedent for a toggle/status-only endpoint.
public class MenuItemAvailabilityResponse
{
    public int Id { get; set; }
    public bool IsAvailable { get; set; }
}
