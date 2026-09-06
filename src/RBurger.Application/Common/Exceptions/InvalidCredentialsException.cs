namespace RBurger.Application.Common.Exceptions;

// Thrown when phone/password do not match on login, and (Day 13) when a refresh token
// presented to POST /api/v1/auth/refresh is missing/invalid/expired/revoked. §7.8 documents
// 401 generically as "Missing/expired JWT"; neither of these specific cases is itemized under
// a status code in §7.8, so reusing 401/INVALID_CREDENTIALS here is an implementation decision
// (flagged in the Day 3 report) rather than a literal documented mapping.
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid phone number or password.")
    {
    }

    public InvalidCredentialsException(string message) : base(message)
    {
    }
}
