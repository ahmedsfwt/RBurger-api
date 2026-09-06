using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Menu.DTOs;
using RBurger.Application.Menu.Queries.GetMenu;

namespace RBurger.Api.Controllers;

// §7.3 Menu & Branches (public, read-only for the apps). Day 15 addition - this documented
// public endpoint had no controller at all before now.
[ApiController]
[Route("api/v1/menu")]
[AllowAnonymous]
public class MenuController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // §7.3 GET /api/v1/menu?branchId=1 - Public. "Full categorized menu for a branch."
    [HttpGet]
    [ProducesResponseType(typeof(List<MenuCategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MenuCategoryResponseDto>>> GetMenu(
        [FromQuery] int branchId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMenuQuery(branchId), cancellationToken);
        return Ok(result);
    }
}
