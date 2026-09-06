using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Models;
using RBurger.Application.Orders.Commands.CreateOrder;
using RBurger.Application.Orders.Commands.CreateReview;
using RBurger.Application.Orders.Commands.CustomerReceived;
using RBurger.Application.Orders.DTOs;
using RBurger.Application.Orders.Queries.GetOrderById;
using RBurger.Application.Orders.Queries.GetOrdersMine;

namespace RBurger.Api.Controllers;

// §7.4 Orders - Customer, plus (Day 7) the Driver branch of GetById. Day 5 added
// customer-received/review to the three Day 4 endpoints.
//
// Class-level [Authorize] only requires "any authenticated JWT" - each action below applies
// its own, narrower [Authorize(Roles = ...)] instead of relying on a class-level Roles list.
// This is deliberate: ASP.NET Core combines multiple [Authorize(Roles = ...)] attributes
// (class + method) with AND semantics, not OR, so a class-level [Authorize(Roles = "Customer")]
// would have silently blocked GetById's new Driver branch even with a method-level
// [Authorize(Roles = "Customer,Driver")] added on top of it. Every action's effective
// authorization requirement is therefore unchanged except for GetById.
[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public OrdersController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    // §7.4 POST /api/v1/orders - Customer JWT · Idempotency-Key header required.
    // §7.8: 201 Created.
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CreateOrderResponse>> Create(
        [FromBody] CreateOrderCommand command,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.CustomerId is null)
        {
            // [Authorize] already blocks unauthenticated requests; defensive guard in case
            // the sub claim is missing/malformed on an otherwise-valid token (see CustomersController).
            return Unauthorized();
        }

        command.CustomerId = _currentUserService.CustomerId.Value;
        command.IdempotencyKey = idempotencyKey;

        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // §7.4 GET /api/v1/orders/mine - Customer JWT · paginated.
    [HttpGet("mine")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(PagedResponse<OrdersMineItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<OrdersMineItemDto>>> Mine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.CustomerId is null)
        {
            return Unauthorized();
        }

        var query = new GetOrdersMineQuery
        {
            CustomerId = _currentUserService.CustomerId.Value,
            Page = page,
            PageSize = pageSize
        };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    // §7.4 GET /api/v1/orders/{orderId} - Customer JWT (own order) or Driver JWT (assigned
    // order). Day 7 completes the previously-deferred Driver branch.
    [HttpGet("{orderId:guid}")]
    [Authorize(Roles = "Customer,Driver")]
    [ProducesResponseType(typeof(OrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailResponse>> GetById(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var customerId = _currentUserService.CustomerId;
        var driverId = _currentUserService.DriverId;

        if (customerId is null && driverId is null)
        {
            // [Authorize(Roles = "Customer,Driver")] already blocks any other/no role;
            // defensive guard in case both sub-claims are missing/malformed.
            return Unauthorized();
        }

        var query = new GetOrderByIdQuery
        {
            OrderId = orderId,
            RequestingCustomerId = customerId,
            RequestingDriverId = driverId
        };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    // §7.4 POST /api/v1/orders/{orderId}/customer-received - Customer JWT (must own the
    // order, Stage must be 3). §7.8: 200 OK (not in the 201-Created enumeration, and no new
    // resource is created).
    [HttpPost("{orderId:guid}/customer-received")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(CustomerReceivedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerReceivedResponse>> CustomerReceived(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.CustomerId is null)
        {
            return Unauthorized();
        }

        var command = new CustomerReceivedCommand
        {
            OrderId = orderId,
            CustomerId = _currentUserService.CustomerId.Value
        };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // §7.4 POST /api/v1/orders/{orderId}/review - Customer JWT (must own the order;
    // customerReceivedAt must be set; one review per order). §7.8: 201 Created (literal).
    [HttpPost("{orderId:guid}/review")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(CreateReviewResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CreateReviewResponse>> CreateReview(
        Guid orderId,
        [FromBody] CreateReviewCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.CustomerId is null)
        {
            return Unauthorized();
        }

        command.OrderId = orderId;
        command.CustomerId = _currentUserService.CustomerId.Value;

        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
