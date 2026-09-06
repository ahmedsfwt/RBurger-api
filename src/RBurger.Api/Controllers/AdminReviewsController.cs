using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Admin.Reviews.Commands.DeleteAdminReview;
using RBurger.Application.Admin.Reviews.DTOs;
using RBurger.Application.Admin.Reviews.Queries.GetAdminReviews;
using RBurger.Application.Common.Models;

namespace RBurger.Api.Controllers;

[ApiController]
[Route("api/v1/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // §7.6.6 GET /api/v1/admin/reviews - Admin JWT, paginated.
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminReviewListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AdminReviewListItemDto>>> GetReviews(
        [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var effectivePage = page == 0 ? 1 : page;
        var effectivePageSize = pageSize == 0 ? 20 : pageSize;

        var result = await _mediator.Send(
            new GetAdminReviewsQuery(effectivePage, effectivePageSize), cancellationToken);
        return Ok(result);
    }

    // §7.6.6 DELETE /api/v1/admin/reviews/{id} - Admin JWT. No documented response body,
    // mirroring every other Admin DELETE endpoint's 204.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteAdminReviewCommand(id), cancellationToken);
        return NoContent();
    }
}
