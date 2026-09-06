using MediatR;
using RBurger.Application.Authentication.DTOs;

namespace RBurger.Application.Authentication.Commands.RefreshToken;

// §7.1 POST /api/v1/auth/refresh - Public (refresh token in body).
// Request: { "refreshToken": "8f2c..." } - field name matches exactly.
public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResponse>;
