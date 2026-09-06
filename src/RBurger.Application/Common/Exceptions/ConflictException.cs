namespace RBurger.Application.Common.Exceptions;

// §7.8: "409 Conflict ... duplicate phone on customer signup". The documentation names the
// scenario but never gives a literal errorCode string value anywhere in §7 - ErrorCode here is
// an implementation decision (flagged in the Day 3 report), only "VALIDATION_ERROR" is a
// literal documented string (§7.8, 400 row).
public class ConflictException : Exception
{
    public string ErrorCode { get; }

    public ConflictException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}
