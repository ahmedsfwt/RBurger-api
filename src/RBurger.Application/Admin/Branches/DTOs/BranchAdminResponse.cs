namespace RBurger.Application.Admin.Branches.DTOs;

// §7.6.2's Create response example shows { id, nameAr, nameEn, deliveryFee, isActive }; the
// Update example shows { id, deliveryFee, isActive }. Per §7.0's "Success envelope: the
// resource itself" and consistent with MenuItemAdminResponse's Day 10 decision, this DTO
// returns the branch's full current state (including etaMinMinutes/etaMaxMinutes, which both
// examples omit but §6.2/§7.3 document as real Branch columns) uniformly from Create/Update.
public class BranchAdminResponse
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public int EtaMinMinutes { get; set; }
    public int EtaMaxMinutes { get; set; }
    public bool IsActive { get; set; }

    // Day 14 addition (Backend Parity Spec §1.4).
    public string? EstimatedDeliveryTime { get; set; }
}

// Day 14 addition (Backend Parity Spec §2.1) - narrow response for the new
// PATCH .../branches/{id}/toggle-status route, mirroring DriverStatusResponse's precedent.
public class BranchStatusResponse
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}
