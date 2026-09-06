using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Branches.DTOs;
using RBurger.Application.Branches.Queries.GetActiveBranches;

namespace RBurger.Api.Controllers;

// §7.3 Menu & Branches (public, read-only for the apps). Day 15 addition - this documented
// public endpoint had no controller at all before now.
[ApiController]
[Route("api/v1/branches")]
[AllowAnonymous]
public class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BranchesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // §7.3 GET /api/v1/branches - Public. "List active branches with fee/ETA - used to
    // populate the branch selector."
    [HttpGet]
    [ProducesResponseType(typeof(List<BranchResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BranchResponse>>> GetBranches(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActiveBranchesQuery(), cancellationToken);
        return Ok(result);
    }
}
