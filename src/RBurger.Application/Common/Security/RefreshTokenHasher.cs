using System.Security.Cryptography;
using System.Text;

namespace RBurger.Application.Common.Security;

// Deterministic SHA-256 hex-digest hashing for opaque refresh tokens (Day 13 addition).
// A refresh token is already 32 bytes of CSPRNG-generated randomness
// (JwtTokenGenerator.GenerateOpaqueRefreshToken), so a fast, unsalted, deterministic hash is
// appropriate here - unlike password hashing (IPasswordHasher), this hash exists purely to let
// POST /api/v1/auth/refresh look up the matching RefreshTokens row by an indexed column
// without ever persisting the raw token value, not to resist a low-entropy guessing attack.
// Pure BCL cryptography - does not violate the "no EF Core/SignalR types in Application" rule.
public static class RefreshTokenHasher
{
    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
