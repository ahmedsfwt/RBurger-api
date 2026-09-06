using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;
using RBurger.Application.Common.Models;

namespace RBurger.Application.Admin.Drivers.Queries.GetDrivers;

// §7.6.3 GET /api/v1/admin/drivers - Admin JWT · paginated (§7.0: page default 1, pageSize
// default 20), mirroring GetOrdersMineQuery's identical shape/defaults.
public class GetDriversQuery : IRequest<PagedResponse<DriverListItemDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
