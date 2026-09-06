using MediatR;
using RBurger.Application.Admin.Branches.DTOs;

namespace RBurger.Application.Admin.Branches.Commands.ToggleBranchStatus;

// Day 14 addition (Backend Parity Spec §2.1). Not documented in v1.2 - §7.6.2 only exposes
// IsActive via PUT /api/v1/admin/branches/{id}'s isActive field. New, additional route
// (does not replace the PUT contract): flips IsActive with no request body.
public record ToggleBranchStatusCommand(int Id) : IRequest<BranchStatusResponse>;
