using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;

namespace RBurger.Application.Admin.Drivers.Commands.ToggleDriverStatus;

// Day 14 addition (Backend Parity Spec §2.3). New, additional route alongside the already-
// documented §7.6.3 PATCH /api/v1/admin/drivers/{id}/status (which requires an explicit
// { isActive } body) - this one flips the driver's current IsActive with no request body.
// Reuses the existing Driver.IsActive column/DriverStatusResponse shape; no second status
// property introduced.
public record ToggleDriverStatusCommand(Guid Id) : IRequest<DriverStatusResponse>;
