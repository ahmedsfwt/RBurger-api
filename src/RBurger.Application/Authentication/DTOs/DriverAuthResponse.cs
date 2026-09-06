namespace RBurger.Application.Authentication.DTOs;

// §7.2 - POST /api/v1/auth/driver/login response shape. Separate from AuthResponse (Customer)
// since the documented fields differ (branchId is present here, and there is no equivalent
// Driver signup response to share this shape with - §7.2 explicitly has no signup endpoint).
public class DriverAuthResponse
{
    public Guid DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string AccessToken { get; set; } = string.Empty;

    // Day 13: a real, persisted (hashed server-side) opaque token, exchangeable via
    // POST /api/v1/auth/refresh (RefreshTokenCommandHandler) - see RefreshToken.cs.
    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresInSeconds { get; set; }
}
