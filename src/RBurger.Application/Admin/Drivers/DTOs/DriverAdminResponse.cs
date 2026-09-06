namespace RBurger.Application.Admin.Drivers.DTOs;

// §7.6.3 Create response example: { driverId, fullName, phone, branchId, isActive }.
// §7.6.3 Update response example: { driverId, vehicle, branchId }.
// Per §7.0's "Success envelope: the resource itself" and consistent with the Day 10 precedent
// already established by BranchAdminResponse/MenuItemAdminResponse (both return the full
// current entity state uniformly from Create/Update, not just the documented example's
// subset), this single DTO covers both endpoints with every field.
public class DriverAdminResponse
{
    public Guid DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Vehicle { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public bool IsActive { get; set; }
}

// §7.6.3 PATCH .../status response: { driverId, isActive } - a deliberately narrow DTO scoped
// to exactly the documented shape, mirroring MenuItemImageDeleteResponse's Day 10 precedent
// for narrow-scoped, single-purpose response shapes (as opposed to DriverAdminResponse's full
// state above, which is only used by Create/Update).
public class DriverStatusResponse
{
    public Guid DriverId { get; set; }
    public bool IsActive { get; set; }
}

// §7.6.3 GET /api/v1/admin/drivers list item example: { driverId, fullName, branchId, vehicle,
// isActive, deliveriesCompleted }.
public class DriverListItemDto
{
    public Guid DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string Vehicle { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DeliveriesCompleted { get; set; }
}
