using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Authentication.DTOs;
using RBurger.Application.Authentication.Queries.GetCustomerMe;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Roles = "Customer")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public CustomersController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    // §7.1 GET /api/v1/customers/me - Customer JWT.
    [HttpGet("me")]
    [ProducesResponseType(typeof(CustomerMeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerMeResponse>> Me(CancellationToken cancellationToken)
    {
        if (_currentUserService.CustomerId is null)
        {
            // [Authorize] already blocks unauthenticated requests; this is a defensive
            // guard in case the sub claim is missing/malformed on an otherwise-valid token.
            return Unauthorized();
        }

        var query = new GetCustomerMeQuery { CustomerId = _currentUserService.CustomerId.Value };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
