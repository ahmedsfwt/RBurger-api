using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Admin.Builder.Commands.CreateBuilderOptionGroup;
using RBurger.Application.Admin.Builder.DTOs;
using RBurger.Application.Admin.Builder.Commands.UpdateBuilderOptionGroup;
using RBurger.Application.Admin.Builder.Commands.DeleteBuilderOptionGroup;

namespace RBurger.Api.Controllers;

[ApiController]
[Route("api/v1/admin/builder")]
[Authorize(Roles = "Admin")]
public class AdminBuilderController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminBuilderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("option-groups")]
    [ProducesResponseType(typeof(BuilderOptionGroupAdminResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BuilderOptionGroupAdminResponse>> CreateOptionGroup(
        [FromBody] CreateBuilderOptionGroupCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("option-groups/{id:int}")]
    [ProducesResponseType(typeof(BuilderOptionGroupAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BuilderOptionGroupAdminResponse>> UpdateOptionGroup(
    int id,
    [FromBody] UpdateBuilderOptionGroupCommand command,
    CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("option-groups/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOptionGroup(int id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteBuilderOptionGroupCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}