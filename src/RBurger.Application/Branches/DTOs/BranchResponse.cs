namespace RBurger.Application.Branches.DTOs;

// §7.3 GET /api/v1/branches response item:
// { "id":1, "nameAr":"سوهاج", "nameEn":"Sohag", "deliveryFee":20, "etaMinMinutes":25, "etaMaxMinutes":35 }
public class BranchResponse
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public int EtaMinMinutes { get; set; }
    public int EtaMaxMinutes { get; set; }

    // Day 15 (Backend Parity §2.4/Day 14): the branch-configured ETA text, additive - the two
    // documented fields above (etaMinMinutes/etaMaxMinutes) are unchanged. This endpoint has no
    // etaSecondsRemaining field at all (that field only exists on the order-detail response,
    // §7.4) so there is no numeric-vs-text conflict to resolve here.
    public string? EstimatedDeliveryTime { get; set; }
}
