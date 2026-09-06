using MediatR;
using RBurger.Application.Admin.Branches.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Branches.Commands.UpdateBranch;

public class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand, BranchAdminResponse>
{
    private readonly IBranchRepository _branchRepository;

    public UpdateBranchCommandHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<BranchAdminResponse> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "branch ... doesn't exist".
        var branch = await _branchRepository.GetByIdAsync(request.Id, cancellationToken);
        if (branch is null)
        {
            throw new NotFoundException($"Branch {request.Id} was not found.");
        }

        var newEtaMin = request.EtaMinMinutes ?? branch.EtaMinMinutes;
        var newEtaMax = request.EtaMaxMinutes ?? branch.EtaMaxMinutes;

        // §1.3's "ETA range" implication (see CreateBranchCommandValidator's comment) applies
        // equally on update - checked here (not the FluentValidation validator) because it
        // needs the branch's existing values whenever only one of the two fields is sent.
        if (newEtaMin > newEtaMax)
        {
            throw new UnprocessableEntityException(
                "EtaMinMinutes must be less than or equal to EtaMaxMinutes.",
                "INVALID_ETA_RANGE");
        }

        if (request.DeliveryFee is not null)
        {
            branch.DeliveryFee = request.DeliveryFee.Value;
        }

        branch.EtaMinMinutes = newEtaMin;
        branch.EtaMaxMinutes = newEtaMax;

        if (request.IsActive is not null)
        {
            branch.IsActive = request.IsActive.Value;
        }

        // Day 14 addition (Backend Parity Spec §1.4).
        if (request.EstimatedDeliveryTime is not null)
        {
            branch.EstimatedDeliveryTime = request.EstimatedDeliveryTime;
        }

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
