using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Admin.Orders.Commands.DeleteAdminOrder;
using RBurger.Application.Admin.Orders.DTOs;
using RBurger.Application.Admin.Orders.Queries.GetAdminOrders;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Models;
using RBurger.Domain.Enums;

namespace RBurger.Api.Controllers;

// §7.6.5 Order Monitoring. Single class-level [Authorize(Roles = "Admin")], mirroring every
// other single-role Admin controller (AdminMenuItemsController, AdminBranchesController, ...).
[ApiController]
[Route("api/v1/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AdminOrdersController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    // §7.6.5 GET /api/v1/admin/orders - Admin JWT, paginated, filterable by
    // branchId/stage/date range. §7.0 pagination defaults (page=1, pageSize=20) applied here,
    // mirroring every other paginated Admin controller action.
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminOrderListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AdminOrderListItemDto>>> GetOrders(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] int? branchId,
        [FromQuery] int? stage,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        // §7.0: "query params page (default 1) and pageSize (default 20)."
        var effectivePage = page == 0 ? 1 : page;
        var effectivePageSize = pageSize == 0 ? 20 : pageSize;

        var query = new GetAdminOrdersQuery(
            effectivePage,
            effectivePageSize,
            branchId,
            stage.HasValue ? (OrderStage)stage.Value : null,
            dateFrom,
            dateTo);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    // §7.6.5 DELETE /api/v1/admin/orders/{id} - Admin JWT. No documented response body for
    // the success case, mirroring every other Admin DELETE endpoint's 204.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        // §9.4: "logged with the acting AdminId" - AdminId is guaranteed non-null here since
        // this action only executes behind [Authorize(Roles = "Admin")] with a valid Admin JWT.
        var adminId = _currentUserService.AdminId!.Value;
        await _mediator.Send(new DeleteAdminOrderCommand(id, adminId), cancellationToken);
        return NoContent();
    }
}
