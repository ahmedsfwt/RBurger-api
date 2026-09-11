using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Admin.Menu.Commands.CreateMenuCategory;
using RBurger.Application.Admin.Menu.Commands.CreateMenuItem;
using RBurger.Application.Admin.Menu.Commands.DeleteMenuItem;
using RBurger.Application.Admin.Menu.Commands.DeleteMenuItemImage;
using RBurger.Application.Admin.Menu.Commands.ToggleMenuItemAvailability;
using RBurger.Application.Admin.Menu.Commands.UpdateMenuItem;
using RBurger.Application.Admin.Menu.Commands.UploadMenuItemImage;
using RBurger.Application.Admin.Menu.DTOs;
using RBurger.Application.Common.Interfaces;


namespace RBurger.Api.Controllers;

// §7.6.1 Menu Management. "Every route below requires an Admin JWT" (§7.6 intro) - a single
// class-level [Authorize(Roles = "Admin")] is safe here (unlike OrdersController) because
// every action in this controller needs exactly the same role, with no dual-role endpoint.
[ApiController]
[Route("api/v1/admin/menu-items")]
[Authorize(Roles = "Admin")]
public class AdminMenuItemsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AdminMenuItemsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    // §7.6.1 POST /api/v1/admin/menu-items - Admin JWT. §7.8: 201 Created.
    [HttpPost]
    [ProducesResponseType(typeof(MenuItemAdminResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemAdminResponse>> Create(
        [FromBody] CreateMenuItemCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // POST /api/v1/admin/menu-items/categories
    [HttpPost("categories")]
    [ProducesResponseType(typeof(MenuCategoryAdminResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MenuCategoryAdminResponse>> CreateCategory(
    [FromBody] CreateMenuCategoryCommand command,
    CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // §7.6.1 PUT /api/v1/admin/menu-items/{id} - Admin JWT. 200 OK (not in §7.8's 201 list).
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MenuItemAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemAdminResponse>> Update(
        int id,
        [FromBody] UpdateMenuItemCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // §7.6.1 DELETE /api/v1/admin/menu-items/{id} - Admin JWT. No documented response body.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteMenuItemCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    // Day 14 addition (Backend Parity Spec §2.2) - not in Documentation v1.2, additional to
    // the documented PUT isAvailable contract. No request body.
    [HttpPatch("{id:int}/toggle-availability")]
    [ProducesResponseType(typeof(MenuItemAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemAvailabilityResponse>> ToggleAvailability(
        int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ToggleMenuItemAvailabilityCommand(id), cancellationToken);
        return Ok(result);
    }

    // §7.6.1 POST /api/v1/admin/menu-items/{id}/image - Admin JWT · multipart/form-data ·
    // Idempotency-Key header required. 200 OK (not in §7.8's 201 list).
    //
    // Deliberately not marked [RequestSizeLimit]/[DisableRequestSizeLimit] beyond the
    // documented 5 MB rule, which is enforced in UploadMenuItemImageCommandValidator against
    // the declared file length - kept in Application per the "business validation separate
    // from framework limits" convention.
    [HttpPost("{id:int}/image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MenuItemImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MenuItemImageUploadResponse>> UploadImage(
        int id,
        IFormFile? file,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        // §7.6.1's request example names the multipart field "file" - IFormFile model-binds
        // it by parameter name "file" automatically, no [FromForm] attribute needed for a
        // single named part.
        var command = new UploadMenuItemImageCommand
        {
            MenuItemId = id,
            Content = file?.OpenReadStream() ?? Stream.Null,
            ContentType = file?.ContentType ?? string.Empty,
            ContentLength = file?.Length ?? 0,
            IdempotencyKey = idempotencyKey,
            // Day 13 addition: scopes this endpoint's idempotency key per-Admin.
            AdminId = _currentUserService.AdminId ?? Guid.Empty
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // §7.6.1 DELETE /api/v1/admin/menu-items/{id}/image - Admin JWT.
    [HttpDelete("{id:int}/image")]
    [ProducesResponseType(typeof(MenuItemImageDeleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemImageDeleteResponse>> DeleteImage(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteMenuItemImageCommand { MenuItemId = id }, cancellationToken);
        return Ok(result);
    }
}
