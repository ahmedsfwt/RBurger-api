using MediatR;
using RBurger.Application.Admin.Branches.DTOs;

namespace RBurger.Application.Admin.Branches.Commands.UpdateBranch;

// §7.6.2 PUT /api/v1/admin/branches/{id}: "Documented update fields: deliveryFee, ETA,
// isActive." NameAr/NameEn are intentionally NOT updatable here - not part of the documented
// contract for this endpoint (Day 10 task brief: "Do not invent additional editable fields").
public class UpdateBranchCommand : IRequest<BranchAdminResponse>
{
    public int Id { get; set; }
    public decimal? DeliveryFee { get; set; }
    public int? EtaMinMinutes { get; set; }
    public int? EtaMaxMinutes { get; set; }
    public bool? IsActive { get; set; }

    // Day 14 addition (Backend Parity Spec §1.4).
    public string? EstimatedDeliveryTime { get; set; }
}
