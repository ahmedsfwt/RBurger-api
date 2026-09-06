using MediatR;
using RBurger.Application.Branches.DTOs;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Branches.Queries.GetActiveBranches;

// §7.3 GET /api/v1/branches - Public. Day 15 addition: this documented, public,
// customer-facing endpoint had no implementation at all before now (only the Admin management
// surface existed).
public record GetActiveBranchesQuery : IRequest<List<BranchResponse>>;

public class GetActiveBranchesQueryHandler : IRequestHandler<GetActiveBranchesQuery, List<BranchResponse>>
{
    private readonly IBranchRepository _branchRepository;

    public GetActiveBranchesQueryHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<List<BranchResponse>> Handle(GetActiveBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.GetActiveBranchesAsync(cancellationToken);

        return branches.Select(b => new BranchResponse
        {
            Id = b.Id,
            NameAr = b.NameAr,
            NameEn = b.NameEn,
            DeliveryFee = b.DeliveryFee,
            EtaMinMinutes = b.EtaMinMinutes,
            EtaMaxMinutes = b.EtaMaxMinutes,
            EstimatedDeliveryTime = b.EstimatedDeliveryTime
        }).ToList();
    }
}
