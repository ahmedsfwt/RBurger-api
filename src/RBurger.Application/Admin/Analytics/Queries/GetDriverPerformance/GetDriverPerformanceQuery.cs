using MediatR;
using RBurger.Application.Admin.Analytics.DTOs;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Analytics.Queries.GetDriverPerformance;

public record GetDriverPerformanceQuery : IRequest<List<DriverPerformanceDto>>, ICacheableQuery
{
    string ICacheableQuery.CacheKey => "analytics:driver-performance";
    int ICacheableQuery.CacheDurationSeconds => 60;
}

// §7.6.6: "Completed-delivery counts per active driver." Reuses the same lifetime
// deliveriesCompleted definition already established for GET /api/v1/admin/drivers
// (GetCompletedDeliveryCountsByDriverIdsAsync, Day 11) - no new formula invented, just scoped
// to IsActive drivers as the route description literally states.
public class GetDriverPerformanceQueryHandler
    : IRequestHandler<GetDriverPerformanceQuery, List<DriverPerformanceDto>>
{
    private readonly IDriverRepository _driverRepository;
    private readonly IOrderRepository _orderRepository;

    public GetDriverPerformanceQueryHandler(
        IDriverRepository driverRepository, IOrderRepository orderRepository)
    {
        _driverRepository = driverRepository;
        _orderRepository = orderRepository;
    }

    public async Task<List<DriverPerformanceDto>> Handle(
        GetDriverPerformanceQuery request, CancellationToken cancellationToken)
    {
        var activeDrivers = await _driverRepository.GetAllActiveAsync(cancellationToken);
        var driverIds = activeDrivers.Select(d => d.Id).ToList();
        var counts = await _orderRepository.GetCompletedDeliveryCountsByDriverIdsAsync(
            driverIds, cancellationToken);

        return activeDrivers
            .Select(d => new DriverPerformanceDto
            {
                DriverId = d.Id,
                FullName = d.FullName,
                DeliveriesCompleted = counts.GetValueOrDefault(d.Id, 0)
            })
            .ToList();
    }
}
