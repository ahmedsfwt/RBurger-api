using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Customers.Commands.DeleteCustomer;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;

    public DeleteCustomerCommandHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "... doesn't exist" - no literal "customer" mention in §7.8's 404 row, but
        // this mirrors every other Admin delete handler's (Branch/Driver) identical guard.
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            throw new NotFoundException($"Customer {request.Id} was not found.");
        }

        // §7.6.4: "Order history rows are retained ... but CustomerId is nulled." A single
        // DeleteAsync + SaveChangesAsync call is sufficient for atomicity: the real EF Core
        // implementation relies on CustomerConfiguration's OnDelete(DeleteBehavior.SetNull)
        // (Day 2 approved decision) so the database nulls out every referencing
        // Order.CustomerId as part of the very same DELETE statement/transaction - see
        // CustomerRepository.DeleteAsync's XML comment.
        await _customerRepository.DeleteAsync(customer, cancellationToken);
        await _customerRepository.SaveChangesAsync(cancellationToken);
    }
}
