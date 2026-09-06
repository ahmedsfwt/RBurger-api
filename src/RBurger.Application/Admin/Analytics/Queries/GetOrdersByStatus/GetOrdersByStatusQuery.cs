using MediatR;
using RBurger.Application.Admin.Analytics.DTOs;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Enums;

namespace RBurger.Application.Admin.Analytics.Queries.GetOrdersByStatus;

public record GetOrdersByStatusQuery : IRequest<List<OrderStatusCountDto>>, ICacheableQuery
{
    string ICacheableQuery.CacheKey => "analytics:orders-by-status";
    int ICacheableQuery.CacheDurationSeconds => 60;
}

// §7.6.6: "Current order counts grouped by Stage 0-3." The example response lists all four
// stages explicitly (including any with count 0), so every OrderStage value is always
// represented in the output, not just stages that currently have at least one order.
public class GetOrdersByStatusQueryHandler
    : IRequestHandler<GetOrdersByStatusQuery, List<OrderStatusCountDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersByStatusQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<List<OrderStatusCountDto>> Handle(
        GetOrdersByStatusQuery request, CancellationToken cancellationToken)
    {
        var counts = await _orderRepository.GetOrderCountsByStatusAsync(cancellationToken);

        return Enum.GetValues<OrderStage>()
            .Select(stage => new OrderStatusCountDto
            {
                Stage = (int)stage,
                Count = counts.GetValueOrDefault(stage, 0)
            })
            .ToList();
    }
}
