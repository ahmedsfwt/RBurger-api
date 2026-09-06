namespace RBurger.Application.Common.Exceptions;

// Defensive fallback only: a valid JWT could reference a Customer row that no longer exists.
// §7.8's 404 row lists "Order/menu item/branch/driver id doesn't exist" but does not explicitly
// itemize this Customer case - included as a necessary, minimal safeguard for
// GET /api/v1/customers/me, flagged in the Day 3 report.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
