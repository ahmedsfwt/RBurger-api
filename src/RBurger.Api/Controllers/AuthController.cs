using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RBurger.Application.Authentication.Commands.AdminLogin;
using RBurger.Application.Authentication.Commands.CustomerLogin;
using RBurger.Application.Authentication.Commands.CustomerSignup;
using RBurger.Application.Authentication.Commands.DriverLogin;
using RBurger.Application.Authentication.Commands.Logout;
using RBurger.Application.Authentication.Commands.RefreshToken;
using RBurger.Application.Authentication.DTOs;

namespace RBurger.Api.Controllers;

// §5.5 (Backend Parity Spec §13): fixed-window rate limiting on every Auth endpoint.
[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // §7.1 POST /api/v1/auth/customer/signup - Public. §7.8: 201 Created.
    [HttpPost("customer/signup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> CustomerSignup(
        [FromBody] CustomerSignupCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // §7.1 POST /api/v1/auth/customer/login - Public. 200 OK (not in §7.8's 201 list).
    [HttpPost("customer/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> CustomerLogin(
        [FromBody] CustomerLoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // §7.2 POST /api/v1/auth/driver/login - Public. 200 OK (not in §7.8's 201 list, mirrors
    // customer/login exactly). Day 6 addition.
    //
    // §7.2 / §1.2 binding rule: this API deliberately has no POST /api/v1/auth/driver/signup
    // route. Do not add one - a Driver JWT can only ever come from logging into an account
    // created via the Admin-only POST /api/v1/admin/drivers (§7.6.3, not yet implemented).
    [HttpPost("driver/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DriverAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DriverAuthResponse>> DriverLogin(
        [FromBody] DriverLoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // §7.6.0 POST /api/v1/auth/admin/login - Public. 200 OK (not in §7.8's 201 list, mirrors
    // customer/login and driver/login exactly). Day 10 addition.
    //
    // §7.6.0 binding note: "Admin accounts have no signup endpoint either ... Only login is
    // public." Do not add a POST /api/v1/auth/admin/signup route.
    [HttpPost("admin/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdminAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminAuthResponse>> AdminLogin(
        [FromBody] AdminLoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // §7.1 POST /api/v1/auth/refresh - Public (refresh token in body). 200 OK. Day 13
    // addition, resolving the Day 3 blocker (see RefreshToken.cs's XML comment) - previously
    // this route did not exist at all. Day 16: response now includes the replacement
    // refreshToken (approved contract change - see RefreshTokenResponse's XML comment).
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // Day 14 addition (Backend Parity Spec §5) - not in Documentation v1.2. See LogoutCommand's
    // XML comment for the request-shape decision (mirrors /refresh's body-based contract
    // exactly). Public/no [Authorize]: the refresh token itself identifies the session.
    // 204: no meaningful response data, and behavior is idempotent regardless of input.
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutCommand command,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
