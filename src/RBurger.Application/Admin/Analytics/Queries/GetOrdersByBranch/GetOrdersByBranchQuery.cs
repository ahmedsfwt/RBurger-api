using MediatR;
using RBurger.Application.Admin.Analytics.DTOs;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Analytics.Queries.GetOrdersByBranch;

public record GetOrdersByBranchQuery : IRequest<List<BranchOrderCountDto>>, ICacheableQuery
{
    string ICacheableQuery.CacheKey => "analytics:orders-by-branch";
    int ICacheableQuery.CacheDurationSeconds => 60;
}

// §7.6.6: "Order counts grouped by branch." Every branch is represented (including branches
// with zero orders), mirroring GetOrdersByStatusQueryHandler's "always show every bucket"
// convention and consistent with the documented 2-branch example matching the current
// Sohag/Girga seed data.
public class GetOrdersByBranchQueryHandler
    : IRequestHandler<GetOrdersByBranchQuery, List<BranchOrderCountDto>>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IOrderRepository _orderRepository;

    public GetOrdersByBranchQueryHandler(
        IBranchRepository branchRepository, IOrderRepository orderRepository)
    {
        _branchRepository = branchRepository;
        _orderRepository = orderRepository;
    }

    public async Task<List<BranchOrderCountDto>> Handle(
        GetOrdersByBranchQuery request, CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.GetAllAsync(cancellationToken);
        var counts = await _orderRepository.GetOrderCountsByBranchAsync(cancellationToken);

        return branches
            .Select(b => new BranchOrderCountDto
            {
                BranchId = b.Id,
                NameAr = b.NameAr,
                NameEn = b.NameEn,
                Count = counts.GetValueOrDefault(b.Id, 0)
            })
            .ToList();
    }
}
