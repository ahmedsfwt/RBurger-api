using MediatR;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Queries.GetDriverMineOrders;

public class GetDriverMineOrdersQueryHandler
    : IRequestHandler<GetDriverMineOrdersQuery, List<DriverOrderMineDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetDriverMineOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<List<DriverOrderMineDto>> Handle(
        GetDriverMineOrdersQuery request, CancellationToken cancellationToken)
    {
        // GetDriverMineOrdersQueryValidator already guarantees Status is "active" or
        // "completed" - anything else is rejected upstream with 400 VALIDATION_ERROR.
        var orders = request.Status == "completed"
            ? await _orderRepository.GetCompletedOrdersForDriverTodayAsync(
                request.DriverId, DateTime.UtcNow.Date, cancellationToken)
            : await _orderRepository.GetActiveOrdersForDriverAsync(request.DriverId, cancellationToken);

        return orders.Select(o => new DriverOrderMineDto
        {
            OrderId = o.Id,
            OrderNumber = o.OrderNumber,
            Stage = o.Stage,
            CustomerAddress = o.Address,
            Total = o.Total
        }).ToList();
    }
}
