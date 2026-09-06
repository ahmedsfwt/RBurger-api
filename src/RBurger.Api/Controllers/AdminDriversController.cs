using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Admin.Drivers.Commands.CreateDriver;
using RBurger.Application.Admin.Drivers.Commands.DeleteDriver;
using RBurger.Application.Admin.Drivers.Commands.ToggleDriverStatus;
using RBurger.Application.Admin.Drivers.Commands.UpdateDriver;
using RBurger.Application.Admin.Drivers.Commands.UpdateDriverStatus;
using RBurger.Application.Admin.Drivers.DTOs;
using RBurger.Application.Admin.Drivers.Queries.GetDrivers;
using RBurger.Application.Common.Models;

namespace RBurger.Api.Controllers;

// §7.6.3 Driver Management - "the only place a Driver account is created". "Every route below
// requires an Admin JWT" (§7.6 intro). Mirrors AdminBranchesController's exact conventions.
[ApiController]
[Route("api/v1/admin/drivers")]
[Authorize(Roles = "Admin")]
public class AdminDriversController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminDriversController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // §7.6.3 POST /api/v1/admin/drivers - Admin JWT. §7.8: 201 Created.
    [HttpPost]
    [ProducesResponseType(typeof(DriverAdminResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DriverAdminResponse>> Create(
        [FromBody] CreateDriverCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // §7.6.3 GET /api/v1/admin/drivers - Admin JWT · paginated.
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DriverListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<DriverListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDriversQuery { Page = page, PageSize = pageSize };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    // §7.6.3 PUT /api/v1/admin/drivers/{id} - Admin JWT. 200 OK (not in §7.8's 201 list).
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DriverAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverAdminResponse>> Update(
        Guid id,
        [FromBody] UpdateDriverCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // §7.6.3 PATCH /api/v1/admin/drivers/{id}/status - Admin JWT.
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(DriverStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverStatusResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateDriverStatusCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // Day 14 addition (Backend Parity Spec §2.3) - not in Documentation v1.2, additional to
    // the documented PATCH .../status (explicit-set) contract above. No request body.
    [HttpPatch("{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(DriverStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverStatusResponse>> ToggleStatus(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ToggleDriverStatusCommand(id), cancellationToken);
        return Ok(result);
    }

    // §7.6.3 DELETE /api/v1/admin/drivers/{id} - Admin JWT. §7.8: 422 if non-terminal
    // (Stage 1-2) assigned orders exist.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteDriverCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
