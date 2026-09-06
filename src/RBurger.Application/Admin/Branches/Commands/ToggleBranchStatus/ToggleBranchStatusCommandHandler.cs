using MediatR;
using RBurger.Application.Admin.Branches.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Branches.Commands.ToggleBranchStatus;

public class ToggleBranchStatusCommandHandler
    : IRequestHandler<ToggleBranchStatusCommand, BranchStatusResponse>
{
    private readonly IBranchRepository _branchRepository;

    public ToggleBranchStatusCommandHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<BranchStatusResponse> Handle(
        ToggleBranchStatusCommand request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(request.Id, cancellationToken);
        if (branch is null)
        {
            throw new NotFoundException($"Branch {request.Id} was not found.");
        }

        branch.IsActive = !branch.IsActive;
        await _branchRepository.SaveChangesAsync(cancellationToken);

        return new BranchStatusResponse
        {
            Id = branch.Id,
            IsActive = branch.IsActive
        };
    }
}
