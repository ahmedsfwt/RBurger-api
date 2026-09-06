using MediatR;
using RBurger.Application.Common.Models;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Queries.GetOrdersMine;

// §7.4 GET /api/v1/orders/mine - Customer JWT · paginated (§7.0: page default 1, pageSize default 20).
public class GetOrdersMineQuery : IRequest<PagedResponse<OrdersMineItemDto>>
{
    public Guid CustomerId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
