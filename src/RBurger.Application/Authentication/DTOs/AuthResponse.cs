namespace RBurger.Application.Authentication.DTOs;

// §7.1 - identical response shape for POST /auth/customer/signup and POST /auth/customer/login.
public class AuthResponse
{
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;

    // Day 13: a real, persisted (hashed server-side) opaque token, now exchangeable via
    // POST /api/v1/auth/refresh (RefreshTokenCommandHandler) - see RefreshToken.cs.
    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresInSeconds { get; set; }
}
