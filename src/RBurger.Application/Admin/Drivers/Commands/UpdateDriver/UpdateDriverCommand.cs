using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;

namespace RBurger.Application.Admin.Drivers.Commands.UpdateDriver;

// §7.6.3 PUT /api/v1/admin/drivers/{id}: "Update a driver's name, vehicle, branch, or
// password. Changing the password here is the only way a driver's credential is ever reset."
// The documented request example only sends a subset ({ vehicle, branchId }) - modeled here as
// all-optional/nullable so any subset of documented fields can be sent, mirroring
// UpdateMenuItemCommand's identical Day 10 partial-update convention. Phone is deliberately
// NOT included: §7.6.3's prose names only "name, vehicle, branch, or password" as editable.
public class UpdateDriverCommand : IRequest<DriverAdminResponse>
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Vehicle { get; set; }
    public int? BranchId { get; set; }
    public string? Password { get; set; }
}
