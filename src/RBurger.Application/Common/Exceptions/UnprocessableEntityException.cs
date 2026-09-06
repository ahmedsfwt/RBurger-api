namespace RBurger.Application.Common.Exceptions;

// §7.8: "422 Unprocessable Entity - Business-rule violation, e.g. backward stage transition,
// deleting a branch/driver that still has non-terminal orders." The doc names the status code
// and gives examples but never a literal errorCode string for the Day 4 order-creation rules
// (unavailable menu item, branch mismatch) - ErrorCode here is an implementation decision,
// flagged using the exact same pattern already established by ConflictException in Day 3.
public class UnprocessableEntityException : Exception
{
    public string ErrorCode { get; }

    public UnprocessableEntityException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}
