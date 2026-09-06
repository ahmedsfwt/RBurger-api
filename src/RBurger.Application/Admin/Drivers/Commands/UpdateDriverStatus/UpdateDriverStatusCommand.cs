using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;

namespace RBurger.Application.Admin.Drivers.Commands.UpdateDriverStatus;

// §7.6.3 PATCH /api/v1/admin/drivers/{id}/status request body: { isActive }.
public class UpdateDriverStatusCommand : IRequest<DriverStatusResponse>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}
