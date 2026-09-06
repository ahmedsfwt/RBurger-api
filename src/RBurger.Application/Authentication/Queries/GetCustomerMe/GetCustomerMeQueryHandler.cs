using Mapster;
using MediatR;
using RBurger.Application.Authentication.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Authentication.Queries.GetCustomerMe;

public class GetCustomerMeQueryHandler : IRequestHandler<GetCustomerMeQuery, CustomerMeResponse>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerMeQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerMeResponse> Handle(GetCustomerMeQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            // Defensive fallback - see NotFoundException's comment.
            throw new NotFoundException("Customer not found.");
        }

        // Mapster: mapping config (Id -> CustomerId, DefaultAddress -> Address) registered in
        // RBurger.Application.DependencyInjection.ServiceCollectionExtensions.AddApplication.
        return customer.Adapt<CustomerMeResponse>();
    }
}
