namespace RBurger.Application.Common.Interfaces;

// Implemented in RBurger.Api (depends on HttpContext), consumed by Application handlers
// that need to know the currently authenticated Customer for GET /api/v1/customers/me.
public interface ICurrentUserService
{
    Guid? CustomerId { get; }

    // Day 7 addition: mirrors CustomerId, scoped to a Driver JWT (role=Driver). Needed now
    // that GetOrderById (§7.4) and the new §7.5 driver endpoints can be reached by a Driver
    // JWT - see RBurger.Api.Common.CurrentUserService for why this must check the "role"
    // claim rather than just reading "sub" unconditionally.
    Guid? DriverId { get; }

    // Day 10 addition: mirrors CustomerId/DriverId exactly, scoped to role=Admin. Needed by
    // the new Admin-only controllers (§7.6) - not strictly required by the documented request
    // shapes (Admin endpoints don't echo the acting AdminId back in any §7.6 response), but
    // kept for symmetry with the existing pattern and for any future audit-trail use
    // (e.g. §9.4's "logged with the acting AdminId", out of scope for Day 10).
    Guid? AdminId { get; }
}
