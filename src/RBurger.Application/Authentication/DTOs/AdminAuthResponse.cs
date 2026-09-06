namespace RBurger.Application.Authentication.DTOs;

// §7.6.0 - POST /api/v1/auth/admin/login response shape: { adminId, name, accessToken,
// refreshToken, expiresInSeconds }. Note the documented field is "name" (mapped from
// Admin.FullName), not "fullName" like DriverAuthResponse - kept literal to the doc example.
public class AdminAuthResponse
{
    public Guid AdminId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;

    // Day 13: a real, persisted (hashed server-side) opaque token, exchangeable via
    // POST /api/v1/auth/refresh (RefreshTokenCommandHandler) - see RefreshToken.cs.
    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresInSeconds { get; set; }
}
