using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;

namespace RBurger.Application.Admin.Drivers.Commands.CreateDriver;

// §7.6.3 POST /api/v1/admin/drivers request body: { fullName, phone, password, vehicle,
// branchId }. This is the single write path that can insert a Drivers row (§6.3) - only
// reachable behind [Authorize(Roles = "Admin")] on the controller.
public class CreateDriverCommand : IRequest<DriverAdminResponse>
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Vehicle { get; set; } = string.Empty;
    public int BranchId { get; set; }
}
