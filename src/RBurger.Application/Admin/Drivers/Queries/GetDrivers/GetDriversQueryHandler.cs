using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Models;

namespace RBurger.Application.Admin.Drivers.Queries.GetDrivers;

public class GetDriversQueryHandler : IRequestHandler<GetDriversQuery, PagedResponse<DriverListItemDto>>
{
    private readonly IDriverRepository _driverRepository;
    private readonly IOrderRepository _orderRepository;

    public GetDriversQueryHandler(IDriverRepository driverRepository, IOrderRepository orderRepository)
    {
        _driverRepository = driverRepository;
        _orderRepository = orderRepository;
    }

    public async Task<PagedResponse<DriverListItemDto>> Handle(
        GetDriversQuery request, CancellationToken cancellationToken)
    {
        // §7.0: "page (default 1) and pageSize (default 20)" - defensive clamp, mirroring
        // GetOrdersMineQueryHandler's identical guard.
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var (drivers, totalCount) = await _driverRepository.GetPagedAsync(page, pageSize, cancellationToken);

        // §7.6.3: "lifetime completed-deliveries count" - batched in a single query across
        // just this page's driver ids, avoiding an N+1 count-per-driver query.
        var driverIds = drivers.Select(d => d.Id).ToList();
        var deliveryCounts = await _orderRepository.GetCompletedDeliveryCountsByDriverIdsAsync(
            driverIds, cancellationToken);

        var items = drivers.Select(d => new DriverListItemDto
        {
            DriverId = d.Id,
            FullName = d.FullName,
            BranchId = d.BranchId,
            Vehicle = d.Vehicle,
            IsActive = d.IsActive,
            DeliveriesCompleted = deliveryCounts.GetValueOrDefault(d.Id, 0)
        }).ToList();

        return new PagedResponse<DriverListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
