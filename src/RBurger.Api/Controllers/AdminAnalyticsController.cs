using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Admin.Analytics.DTOs;
using RBurger.Application.Admin.Analytics.Queries.GetAnalyticsOverview;
using RBurger.Application.Admin.Analytics.Queries.GetDriverPerformance;
using RBurger.Application.Admin.Analytics.Queries.GetOrdersByBranch;
using RBurger.Application.Admin.Analytics.Queries.GetOrdersByStatus;
using RBurger.Application.Admin.Analytics.Queries.GetRatingDistribution;
using RBurger.Application.Admin.Analytics.Queries.GetRevenueTrend;
using RBurger.Application.Admin.Analytics.Queries.GetTopItems;

namespace RBurger.Api.Controllers;

// §7.6.6 Reviews & Analytics (analytics half). All 7 documented routes are now implemented.
// /overview (Day 15, Backend Parity Spec §11) returns a fully real response except
// completionRatePercent/deltas.completionRate, which remain null - that formula is still
// undefined anywhere in Documentation v1.2 and no approved spec has defined one; the field is
// isolated rather than blocking the whole endpoint (see GetAnalyticsOverviewQueryHandler).
[ApiController]
[Route("api/v1/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // §7.6.6 GET /api/v1/admin/analytics/overview?range=today|7d|30d - Day 15: implemented
    // (see AnalyticsOverviewDto's XML comment for the one isolated null field).
    [HttpGet("overview")]
    [ProducesResponseType(typeof(AnalyticsOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnalyticsOverviewDto>> GetOverview(
        [FromQuery] string range, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAnalyticsOverviewQuery(range), cancellationToken);
        return Ok(result);
    }

    // §7.6.6 GET /api/v1/admin/analytics/revenue-trend?days=7 - Day 14: implemented (Backend
    // Parity Spec §1.1 resolved the revenue-basis blocker). See GetRevenueTrendQueryHandler.
    [HttpGet("revenue-trend")]
    [ProducesResponseType(typeof(List<RevenueTrendPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<RevenueTrendPointDto>>> GetRevenueTrend(
        [FromQuery] int days, CancellationToken cancellationToken)
    {
        var effectiveDays = days == 0 ? 7 : days;
        var result = await _mediator.Send(new GetRevenueTrendQuery(effectiveDays), cancellationToken);
        return Ok(result);
    }

    // §7.6.6 GET /api/v1/admin/analytics/orders-by-status - fully implemented.
    [HttpGet("orders-by-status")]
    [ProducesResponseType(typeof(List<OrderStatusCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<OrderStatusCountDto>>> GetOrdersByStatus(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrdersByStatusQuery(), cancellationToken);
        return Ok(result);
    }

    // §7.6.6 GET /api/v1/admin/analytics/orders-by-branch - fully implemented.
    [HttpGet("orders-by-branch")]
    [ProducesResponseType(typeof(List<BranchOrderCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<BranchOrderCountDto>>> GetOrdersByBranch(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrdersByBranchQuery(), cancellationToken);
        return Ok(result);
    }

    // §7.6.6 GET /api/v1/admin/analytics/top-items?limit=5 - fully implemented.
    [HttpGet("top-items")]
    [ProducesResponseType(typeof(List<TopSellingItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<TopSellingItemDto>>> GetTopItems(
        [FromQuery] int limit, CancellationToken cancellationToken)
    {
        var effectiveLimit = limit == 0 ? 5 : limit;
        var result = await _mediator.Send(new GetTopItemsQuery(effectiveLimit), cancellationToken);
        return Ok(result);
    }

    // §7.6.6 GET /api/v1/admin/analytics/driver-performance - fully implemented.
    [HttpGet("driver-performance")]
    [ProducesResponseType(typeof(List<DriverPerformanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<DriverPerformanceDto>>> GetDriverPerformance(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDriverPerformanceQuery(), cancellationToken);
        return Ok(result);
    }

    // §7.6.6 GET /api/v1/admin/analytics/rating-distribution - fully implemented.
    [HttpGet("rating-distribution")]
    [ProducesResponseType(typeof(List<RatingDistributionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<RatingDistributionDto>>> GetRatingDistribution(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRatingDistributionQuery(), cancellationToken);
        return Ok(result);
    }
}
