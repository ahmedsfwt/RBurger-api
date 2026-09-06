using MediatR;
using RBurger.Application.Authentication.DTOs;

namespace RBurger.Application.Authentication.Commands.AdminLogin;

// §7.6.0 POST /api/v1/auth/admin/login request body - fields match exactly: { username,
// password }. Mirrors DriverLoginCommand/CustomerLoginCommand's shape.
public class AdminLoginCommand : IRequest<AdminAuthResponse>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
