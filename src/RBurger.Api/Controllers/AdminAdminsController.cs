using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Admin.Admins.Commands.CreateAdmin;
using RBurger.Application.Admin.Admins.DTOs;

namespace RBurger.Api.Controllers;

[ApiController]
[Route("api/v1/admin/admins")]
[Authorize(Roles = "Admin")]
public class AdminAdminsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAdminsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/v1/admin/admins - Admin JWT. Creates a new admin account
    // (not in the documented spec; internal-only extension so more than the seeded
    // "admin" account can be provisioned without a manual migration).
    [HttpPost]
    [ProducesResponseType(typeof(AdminAdminResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminAdminResponse>> Create(
        [FromBody] CreateAdminCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}