namespace RBurger.Infrastructure.Authentication;

// Day 13 addition. Documentation v1.2 documents that a refresh token exists and is exchanged
// via POST /api/v1/auth/refresh (§7.1), but nowhere specifies its lifetime/TTL - only the
// access token's ExpiresInSeconds=3600 is documented (§7.1 response examples). A refresh
// token's expiration is a technical/security necessity to implement the explicitly-requested
// "support expiration" requirement, not a documented business rule, so - mirroring
// JwtSettings' exact precedent for Issuer/Audience/Key ("values must be supplied via
// configuration... not documented anywhere") - this is exposed as configuration with a
// reasonable default (30 days, a common industry default for a mobile/web food-delivery app),
// flagged here and in the Day 13 report as an assumption Ahmed can override via configuration
// at any time without a code change.
public class RefreshTokenSettings
{
    public const string SectionName = "RefreshToken";

    public int ExpiresInDays { get; set; } = 30;
}
