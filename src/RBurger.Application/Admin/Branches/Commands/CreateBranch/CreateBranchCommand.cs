using MediatR;
using RBurger.Application.Admin.Branches.DTOs;

namespace RBurger.Application.Admin.Branches.Commands.CreateBranch;

// §7.6.2 POST /api/v1/admin/branches request body: { nameAr, nameEn, deliveryFee,
// etaMinMinutes, etaMaxMinutes }.
//
// Note: BranchConfiguration.HotlinePhones is required (Day 2 approved decision #5) but is NOT
// part of this documented request body either - unlike MenuItem.BranchId this does not need
// Ahmed's Blocking-Issue-style approval, because HotlinePhones is a plain nvarchar column
// (not an FK), and Branch.HotlinePhones already defaults to string.Empty in the Domain
// entity - an empty string satisfies the NOT NULL/required column constraint on its own, with
// no fallback-selection ambiguity like BranchId's FK reference had. Left unset (empty) on
// create; can be added to this contract later if the documentation is updated.
public class CreateBranchCommand : IRequest<BranchAdminResponse>
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public int EtaMinMinutes { get; set; }
    public int EtaMaxMinutes { get; set; }

    // Day 14 addition (Backend Parity Spec §1.4) - not part of §7.6.2's original documented
    // request body, optional (nullable), mirrors HotlinePhones' precedent above for a plain,
    // non-FK column added outside the original documented contract.
    public string? EstimatedDeliveryTime { get; set; }
}
