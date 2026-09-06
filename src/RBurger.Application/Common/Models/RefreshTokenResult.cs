namespace RBurger.Application.Common.Models;

// Day 13 addition, mirrors JwtTokenResult's exact shape. ExpiresAt is computed in
// Infrastructure (JwtTokenGenerator, using RefreshTokenSettings - see its XML comment for why
// the TTL itself is undocumented configuration), so Application handlers never need to know
// the configured duration, only the resulting values.
public record RefreshTokenResult(string RawToken, DateTime ExpiresAt);
