using MediatR;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Queries.GetDriverMineOrders;

// §7.5 GET /api/v1/driver/orders/mine?status=active|completed - Driver JWT.
public class GetDriverMineOrdersQuery : IRequest<List<DriverOrderMineDto>>
{
    // Not part of a documented query param - populated by DriverOrdersController from the
    // authenticated JWT's "sub" claim (§5.4).
    public Guid DriverId { get; set; }

    // §7.5 literal query param: status=active│completed. Validated to be exactly one of
    // these two documented values by GetDriverMineOrdersQueryValidator.
    public string Status { get; set; } = string.Empty;
}
