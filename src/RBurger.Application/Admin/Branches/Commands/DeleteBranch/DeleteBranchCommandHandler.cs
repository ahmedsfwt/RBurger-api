using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Branches.Commands.DeleteBranch;

public class DeleteBranchCommandHandler : IRequestHandler<DeleteBranchCommand>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDriverRepository _driverRepository;

    public DeleteBranchCommandHandler(
        IBranchRepository branchRepository,
        IOrderRepository orderRepository,
        IDriverRepository driverRepository)
    {
        _branchRepository = branchRepository;
        _orderRepository = orderRepository;
        _driverRepository = driverRepository;
    }

    public async Task Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "branch ... doesn't exist".
        var branch = await _branchRepository.GetByIdAsync(request.Id, cancellationToken);
        if (branch is null)
        {
            throw new NotFoundException($"Branch {request.Id} was not found.");
        }

        // §7.6.2: "Rejected with 422 if the branch still has non-terminal (Stage 0-2) orders
        // or any assigned drivers - deactivate instead of deleting in that case."
        if (await _orderRepository.HasNonTerminalOrdersForBranchAsync(request.Id, cancellationToken))
        {
            throw new UnprocessableEntityException(
                $"Branch {request.Id} still has non-terminal orders and cannot be deleted. Deactivate it instead.",
                "BRANCH_HAS_NON_TERMINAL_ORDERS");
        }

        if (await _driverRepository.HasDriversForBranchAsync(request.Id, cancellationToken))
        {
            throw new UnprocessableEntityException(
                $"Branch {request.Id} still has assigned drivers and cannot be deleted. Deactivate it instead.",
                "BRANCH_HAS_ASSIGNED_DRIVERS");
        }

        // BranchRepository.DeleteAsync also carries a defensive fallback (undocumented
        // MenuItems-FK-Restrict edge case) - see IBranchRepository's XML comment.
        await _branchRepository.DeleteAsync(branch, cancellationToken);
        await _branchRepository.SaveChangesAsync(cancellationToken);
    }
}
