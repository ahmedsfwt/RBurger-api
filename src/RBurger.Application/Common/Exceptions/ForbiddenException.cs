namespace RBurger.Application.Common.Exceptions;

// §7.8: "403 Forbidden - Valid JWT but wrong role or not the resource owner". Used here for
// the "not the resource owner" case (e.g. a Customer JWT requesting another customer's order
// via GET /api/v1/orders/{orderId}). New addition, following the exact same minimal pattern
// already established by NotFoundException/ConflictException in Day 3.
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
