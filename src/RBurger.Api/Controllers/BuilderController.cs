using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Builder.Queries.GetBuilderOptions;

namespace RBurger.Api.Controllers;

// §7.3 GET /api/v1/builder/options - Public. Day 15 addition - this documented public endpoint
// had no controller at all before now.
[ApiController]
[Route("api/v1/builder")]
[AllowAnonymous]
public class BuilderController : ControllerBase
{
    private readonly IMediator _mediator;

    public BuilderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("options")]
    [ProducesResponseType(typeof(List<BuilderOptionGroupResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BuilderOptionGroupResponseDto>>> GetOptions(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBuilderOptionsQuery(), cancellationToken);
        return Ok(result);
    }
}
