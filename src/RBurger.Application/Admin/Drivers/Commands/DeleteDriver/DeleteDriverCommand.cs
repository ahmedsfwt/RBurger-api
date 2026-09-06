using MediatR;

namespace RBurger.Application.Admin.Drivers.Commands.DeleteDriver;

// §7.6.3 DELETE /api/v1/admin/drivers/{id}.
public class DeleteDriverCommand : IRequest
{
    public Guid Id { get; set; }
}
