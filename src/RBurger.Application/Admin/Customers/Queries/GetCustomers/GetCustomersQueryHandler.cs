using MediatR;
using RBurger.Application.Admin.Customers.DTOs;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Models;

namespace RBurger.Application.Admin.Customers.Queries.GetCustomers;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PagedResponse<CustomerListItemDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderRepository _orderRepository;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository, IOrderRepository orderRepository)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
    }

    public async Task<PagedResponse<CustomerListItemDto>> Handle(
        GetCustomersQuery request, CancellationToken cancellationToken)
    {
        // §7.0 defensive clamp, mirroring GetDriversQueryHandler's identical guard.
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var (customers, totalCount) = await _customerRepository.GetPagedAsync(page, pageSize, cancellationToken);

        // §7.6.4: "lifetime order count" - batched across just this page's customer ids,
        // mirroring GetDriversQueryHandler's delivery-count batching.
        var customerIds = customers.Select(c => c.Id).ToList();
        var orderCounts = await _orderRepository.GetOrderCountsByCustomerIdsAsync(customerIds, cancellationToken);

        var items = customers.Select(c => new CustomerListItemDto
        {
            CustomerId = c.Id,
            FullName = c.FullName,
            Phone = c.Phone,
            OrdersCount = orderCounts.GetValueOrDefault(c.Id, 0)
        }).ToList();

        return new PagedResponse<CustomerListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
