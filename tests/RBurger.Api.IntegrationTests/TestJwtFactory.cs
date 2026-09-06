using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RBurger.Infrastructure.Authentication;

namespace RBurger.Api.IntegrationTests;

// Test-only helper that mirrors RBurger.Infrastructure.Authentication.JwtTokenGenerator's
// exact claim shape (sub, role). Resolves the actual JwtSettings the test host will validate
// against directly from its DI container (the same IOptions<JwtSettings> registered by
// AddInfrastructure) so the minted token always matches whatever configuration is actually in
// effect.
//
// Day 16 update: RBurgerTestWebApplicationFactory now injects a fixed, non-empty, test-only
// Jwt:Key (Program.cs fails fast on a missing key rather than silently falling back to one -
// Backend Parity Spec §4), so jwtSettings.Key here is always guaranteed non-empty and no local
// fallback logic is needed in this helper anymore.
internal static class TestJwtFactory
{
    public static string CreateToken(RBurgerTestWebApplicationFactory factory, Guid subjectId, string role)
    {
        using var scope = factory.Services.CreateScope();
        var jwtSettings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new Claim("role", role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Day 9 addition (§13.4 "invalid JWT"). Deliberately does NOT read the test host's real
    // JwtSettings - the whole point is a token that the API's configured signing key will
    // reject. Issuer/audience are left empty since a signature failure alone is sufficient to
    // prove the token is untrusted; TokenValidationParameters rejects on signature before it
    // would ever reach issuer/audience checks.
    public static string CreateTokenWithWrongSigningKey(Guid subjectId, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new Claim("role", role)
        };

        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('9', 32)));
        var credentials = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
