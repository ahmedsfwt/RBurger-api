using MediatR;

namespace RBurger.Application.Admin.Customers.Commands.DeleteCustomer;

// §7.6.4 DELETE /api/v1/admin/customers/{id}.
public class DeleteCustomerCommand : IRequest
{
    public Guid Id { get; set; }
}
