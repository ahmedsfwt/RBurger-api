namespace RBurger.Infrastructure.Authentication;

// Issuer/Audience/signing Key are NOT documented anywhere in the Technical & Product
// Documentation v1.2 - values must be supplied via configuration (User Secrets locally,
// Secrets Manager/Parameter Store per §10.1 in production). ExpiresInSeconds=3600 IS
// documented (§7.1 response examples) and is used as the default here.
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; } = 3600;
}
