using MediatR;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Models;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Queries.GetOrdersMine;

public class GetOrdersMineQueryHandler : IRequestHandler<GetOrdersMineQuery, PagedResponse<OrdersMineItemDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersMineQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<PagedResponse<OrdersMineItemDto>> Handle(
        GetOrdersMineQuery request, CancellationToken cancellationToken)
    {
        // §7.0: "page (default 1) and pageSize (default 20)" - defensive clamp against
        // out-of-range query params rather than letting them reach Skip/Take unchecked.
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var (orders, totalCount) = await _orderRepository.GetCustomerOrdersPagedAsync(
            request.CustomerId, page, pageSize, cancellationToken);

        var items = orders.Select(o => new OrdersMineItemDto
        {
            OrderId = o.Id,
            OrderNumber = o.OrderNumber,
            BranchNameAr = o.Branch.NameAr,
            Stage = o.Stage,
            Total = o.Total,
            CustomerReceivedAt = o.CustomerReceivedAt,
            HasReview = o.Review is not null
        }).ToList();

        return new PagedResponse<OrdersMineItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
