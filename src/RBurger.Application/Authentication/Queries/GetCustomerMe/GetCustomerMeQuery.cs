using MediatR;
using RBurger.Application.Authentication.DTOs;

namespace RBurger.Application.Authentication.Queries.GetCustomerMe;

// §7.1 GET /api/v1/customers/me - CustomerId comes from the authenticated JWT's sub claim,
// resolved via ICurrentUserService at the API layer.
public class GetCustomerMeQuery : IRequest<CustomerMeResponse>
{
    public Guid CustomerId { get; set; }
}
