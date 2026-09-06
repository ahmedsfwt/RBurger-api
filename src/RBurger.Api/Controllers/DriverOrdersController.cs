using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.Commands.DeliverOrder;
using RBurger.Application.Orders.Commands.ReceiveOrder;
using RBurger.Application.Orders.Commands.ShipOrder;
using RBurger.Application.Orders.DTOs;
using RBurger.Application.Orders.Queries.GetDriverMineOrders;
using RBurger.Application.Orders.Queries.GetDriverNewOrders;

namespace RBurger.Api.Controllers;

// §7.5 Orders - Driver (the delivery-status triggers). Day 7 addition.
[ApiController]
[Route("api/v1/driver/orders")]
[Authorize(Roles = "Driver")]
public class DriverOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public DriverOrdersController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    // §7.5 GET /api/v1/driver/orders/new - Driver JWT. Literal response shape: bare array
    // (approved Day 7 decision #2 - no PagedResponse<T> envelope).
    [HttpGet("new")]
    [ProducesResponseType(typeof(List<DriverNewOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DriverNewOrderDto>>> New(CancellationToken cancellationToken)
    {
        if (_currentUserService.DriverId is null)
        {
            // [Authorize(Roles = "Driver")] already blocks unauthenticated/wrong-role
            // requests; defensive guard in case the sub claim is missing/malformed on an
            // otherwise-valid token (see CustomersController/OrdersController).
            return Unauthorized();
        }

        var query = new GetDriverNewOrdersQuery { DriverId = _currentUserService.DriverId.Value };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    // §7.5 GET /api/v1/driver/orders/mine?status=active|completed - Driver JWT. Literal
    // response shape: bare array (approved Day 7 decision #2 - no PagedResponse<T> envelope).
    [HttpGet("mine")]
    [ProducesResponseType(typeof(List<DriverOrderMineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<DriverOrderMineDto>>> Mine(
        [FromQuery] string status,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.DriverId is null)
        {
            return Unauthorized();
        }

        var query = new GetDriverMineOrdersQuery
        {
            DriverId = _currentUserService.DriverId.Value,
            Status = status
        };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    // §7.5 POST /api/v1/driver/orders/{orderId}/receive - Driver JWT. §7.8: 200 OK (no new
    // resource created); 409 Conflict if another driver already received it.
    [HttpPost("{orderId:guid}/receive")]
    [ProducesResponseType(typeof(ReceiveOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReceiveOrderResponse>> Receive(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.DriverId is null)
        {
            return Unauthorized();
        }

        var command = new ReceiveOrderCommand { OrderId = orderId, DriverId = _currentUserService.DriverId.Value };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // §7.5 POST /api/v1/driver/orders/{orderId}/ship - Driver JWT (must be the assigned
    // driver). §7.8: 200 OK; 403 if not the assigned driver; 422 for an out-of-sequence call
    // (approved Day 7 decision #4).
    [HttpPost("{orderId:guid}/ship")]
    [ProducesResponseType(typeof(ShipOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ShipOrderResponse>> Ship(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.DriverId is null)
        {
            return Unauthorized();
        }

        var command = new ShipOrderCommand { OrderId = orderId, DriverId = _currentUserService.DriverId.Value };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // §7.5 POST /api/v1/driver/orders/{orderId}/deliver - Driver JWT (must be the assigned
    // driver). §7.8: 200 OK; 403 if not the assigned driver; 422 for an out-of-sequence call
    // (approved Day 7 decision #4). §9.1: cash Payment.Status auto-captured on success.
    [HttpPost("{orderId:guid}/deliver")]
    [ProducesResponseType(typeof(DeliverOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DeliverOrderResponse>> Deliver(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.DriverId is null)
        {
            return Unauthorized();
        }

        var command = new DeliverOrderCommand { OrderId = orderId, DriverId = _currentUserService.DriverId.Value };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
