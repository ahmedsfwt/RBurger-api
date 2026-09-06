using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Admin.Customers.Commands.DeleteCustomer;
using RBurger.Application.Admin.Customers.DTOs;
using RBurger.Application.Admin.Customers.Queries.GetCustomers;
using RBurger.Application.Common.Models;

namespace RBurger.Api.Controllers;

// §7.6.4 Customer Management (view + delete only - accounts are self-service, §7.1). "Every
// route below requires an Admin JWT" (§7.6 intro). Mirrors AdminBranchesController's exact
// conventions.
[ApiController]
[Route("api/v1/admin/customers")]
[Authorize(Roles = "Admin")]
public class AdminCustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // §7.6.4 GET /api/v1/admin/customers - Admin JWT · paginated.
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CustomerListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<CustomerListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCustomersQuery { Page = page, PageSize = pageSize };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    // §7.6.4 DELETE /api/v1/admin/customers/{id} - Admin JWT. GDPR-style erasure: order
    // history retained, Order.CustomerId nulled (§6.2, CustomerConfiguration's SetNull).
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCustomerCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
