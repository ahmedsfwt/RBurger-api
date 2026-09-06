using MediatR;
using RBurger.Application.Admin.Customers.DTOs;
using RBurger.Application.Common.Models;

namespace RBurger.Application.Admin.Customers.Queries.GetCustomers;

// §7.6.4 GET /api/v1/admin/customers - Admin JWT · paginated, mirroring GetDriversQuery's
// identical shape/defaults.
public class GetCustomersQuery : IRequest<PagedResponse<CustomerListItemDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
