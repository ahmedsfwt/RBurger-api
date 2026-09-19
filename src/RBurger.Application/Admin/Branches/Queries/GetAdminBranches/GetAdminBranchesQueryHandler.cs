using MediatR;
using RBurger.Application.Admin.Branches.DTOs;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Branches.Queries.GetAdminBranches;

public class GetAdminBranchesQueryHandler : IRequestHandler<GetAdminBranchesQuery, List<BranchAdminResponse>>
{
    private readonly IBranchRepository _branchRepository;

    public GetAdminBranchesQueryHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<List<BranchAdminResponse>> Handle(GetAdminBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.GetAllAsync(cancellationToken);

        return branches.Select(b => new BranchAdminResponse
        {
            Id = b.Id,
            NameAr = b.NameAr,
            NameEn = b.NameEn,
            DeliveryFee = b.DeliveryFee,
            EtaMinMinutes = b.EtaMinMinutes,
            EtaMaxMinutes = b.EtaMaxMinutes,
            IsActive = b.IsActive,
            HotlinePhones = b.HotlinePhones,
            EstimatedDeliveryTime = b.EstimatedDeliveryTime
        }).ToList();
    }
}