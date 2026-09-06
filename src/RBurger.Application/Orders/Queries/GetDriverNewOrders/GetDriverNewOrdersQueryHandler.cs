using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Queries.GetDriverNewOrders;

public class GetDriverNewOrdersQueryHandler
    : IRequestHandler<GetDriverNewOrdersQuery, List<DriverNewOrderDto>>
{
    private readonly IDriverRepository _driverRepository;
    private readonly IOrderRepository _orderRepository;

    public GetDriverNewOrdersQueryHandler(IDriverRepository driverRepository, IOrderRepository orderRepository)
    {
        _driverRepository = driverRepository;
        _orderRepository = orderRepository;
    }

    public async Task<List<DriverNewOrderDto>> Handle(
        GetDriverNewOrdersQuery request, CancellationToken cancellationToken)
    {
        // §7.5: "at the driver's own branch" - the Driver JWT carries no branchId claim
        // (approved Day 6 decision), so the driver's branch must be resolved server-side.
        var driver = await _driverRepository.GetByIdAsync(request.DriverId, cancellationToken);
        if (driver is null)
        {
            // §7.8: 404 "...driver id doesn't exist". Defensive - [Authorize(Roles="Driver")]
            // plus a validly-issued JWT means this should not normally happen.
            throw new NotFoundException($"Driver {request.DriverId} was not found.");
        }

        var orders = await _orderRepository.GetNewOrdersForBranchAsync(driver.BranchId, cancellationToken);

        return orders.Select(o => new DriverNewOrderDto
        {
            OrderId = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerAddress = o.Address,
            Total = o.Total,
            Notes = o.Notes
        }).ToList();
    }
}
