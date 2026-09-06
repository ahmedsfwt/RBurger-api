using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Models;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly RefreshTokenSettings _refreshTokenSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> options, IOptions<RefreshTokenSettings> refreshTokenOptions)
    {
        _settings = options.Value;
        _refreshTokenSettings = refreshTokenOptions.Value;
    }

    public JwtTokenResult GenerateAccessToken(Customer customer)
    {
        // Approved decision #2: only sub = CustomerId and role = Customer. "role" is used
        // literally (not ClaimTypes.Role's long URI form) to match §5.4's documented
        // "role=Customer" claim exactly; TokenValidationParameters.RoleClaimType is set to
        // "role" accordingly in RBurger.Api/Program.cs.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new("role", "Customer")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddSeconds(_settings.ExpiresInSeconds);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(accessToken, _settings.ExpiresInSeconds);
    }

    // Day 6 addition: mirrors the Customer overload exactly. Approved Day 6 decision #3: only
    // sub = DriverId and role = Driver - no branchId claim (see IJwtTokenGenerator's XML comment).
    public JwtTokenResult GenerateAccessToken(Driver driver)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, driver.Id.ToString()),
            new("role", "Driver")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddSeconds(_settings.ExpiresInSeconds);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(accessToken, _settings.ExpiresInSeconds);
    }

    // Day 10 addition: mirrors the Customer/Driver overloads exactly. Approved decision: only
    // sub = AdminId and role = Admin (§5.4, §7.6.0) - no additional claims.
    public JwtTokenResult GenerateAccessToken(Admin admin)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new("role", "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddSeconds(_settings.ExpiresInSeconds);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(accessToken, _settings.ExpiresInSeconds);
    }

    public RefreshTokenResult GenerateOpaqueRefreshToken()
    {
        // Day 13: now actually persisted (hashed) and validated by POST /api/v1/auth/refresh -
        // see IJwtTokenGenerator's XML comment.
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenSettings.ExpiresInDays);
        return new RefreshTokenResult(rawToken, expiresAt);
    }
}
