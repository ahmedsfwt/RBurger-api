namespace RBurger.Api.Common;

// Day 15 addition (Backend Parity Spec §13). §5.5: "Rate limiting (fixed window) on Auth and
// Payment endpoints" - no specific limit values are documented anywhere in v1.2, so they are
// exposed as configuration (mirrors JwtSettings/RefreshTokenSettings' exact precedent for
// undocumented-but-necessary values) rather than hardcoded, with reasonable defaults.
public class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitLimit { get; set; } = 10;
    public int AuthWindowSeconds { get; set; } = 60;

    public int PaymentPermitLimit { get; set; } = 20;
    public int PaymentWindowSeconds { get; set; } = 60;
}
