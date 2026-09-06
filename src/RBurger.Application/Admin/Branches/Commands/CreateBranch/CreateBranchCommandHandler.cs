using MediatR;
using RBurger.Application.Admin.Branches.DTOs;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Admin.Branches.Commands.CreateBranch;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, BranchAdminResponse>
{
    private readonly IBranchRepository _branchRepository;

    public CreateBranchCommandHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<BranchAdminResponse> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = new Branch
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            DeliveryFee = request.DeliveryFee,
            EtaMinMinutes = request.EtaMinMinutes,
            EtaMaxMinutes = request.EtaMaxMinutes,
            HotlinePhones = string.Empty, // see CreateBranchCommand's XML comment
            IsActive = true, // §6.2: "bit | default 1"
            EstimatedDeliveryTime = request.EstimatedDeliveryTime // Day 14 (Backend Parity Spec §1.4)
        };

        await _branchRepository.AddAsync(branch, cancellationToken);
        await _branchRepository.SaveChangesAsync(cancellationToken);

        return new BranchAdminResponse
        {
            Id = branch.Id,
            NameAr = branch.NameAr,
            NameEn = branch.NameEn,
            DeliveryFee = branch.DeliveryFee,
            EtaMinMinutes = branch.EtaMinMinutes,
            EtaMaxMinutes = branch.EtaMaxMinutes,
            IsActive = branch.IsActive,
            EstimatedDeliveryTime = branch.EstimatedDeliveryTime
        };
    }
}
