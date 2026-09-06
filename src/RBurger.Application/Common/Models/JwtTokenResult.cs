namespace RBurger.Application.Common.Models;

public record JwtTokenResult(string AccessToken, int ExpiresInSeconds);
