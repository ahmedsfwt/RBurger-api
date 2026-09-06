using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Admin.Branches.Commands.CreateBranch;
using RBurger.Application.Admin.Branches.Commands.DeleteBranch;
using RBurger.Application.Admin.Branches.Commands.ToggleBranchStatus;
using RBurger.Application.Admin.Branches.Commands.UpdateBranch;
using RBurger.Application.Admin.Branches.DTOs;

namespace RBurger.Api.Controllers;

// §7.6.2 Branch Management. "Every route below requires an Admin JWT" (§7.6 intro).
[ApiController]
[Route("api/v1/admin/branches")]
[Authorize(Roles = "Admin")]
public class AdminBranchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminBranchesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // §7.6.2 POST /api/v1/admin/branches - Admin JWT. §7.8: 201 Created.
    [HttpPost]
    [ProducesResponseType(typeof(BranchAdminResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BranchAdminResponse>> Create(
        [FromBody] CreateBranchCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // §7.6.2 PUT /api/v1/admin/branches/{id} - Admin JWT. 200 OK (not in §7.8's 201 list).
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BranchAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BranchAdminResponse>> Update(
        int id,
        [FromBody] UpdateBranchCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // Day 14 addition (Backend Parity Spec §2.1) - not in Documentation v1.2, additional to
    // the documented PUT isActive contract. No request body.
    [HttpPatch("{id:int}/toggle-status")]
    [ProducesResponseType(typeof(BranchStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchStatusResponse>> ToggleStatus(
        int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ToggleBranchStatusCommand(id), cancellationToken);
        return Ok(result);
    }

    // §7.6.2 DELETE /api/v1/admin/branches/{id} - Admin JWT. §7.8: 422 if non-terminal orders
    // or assigned drivers exist.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteBranchCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
