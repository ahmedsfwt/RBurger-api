using MediatR;
using RBurger.Application.Authentication.DTOs;

namespace RBurger.Application.Authentication.Commands.CustomerSignup;

// §7.1 POST /api/v1/auth/customer/signup request body - fields match exactly.
public class CustomerSignupCommand : IRequest<AuthResponse>
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string PreferredLanguage { get; set; } = string.Empty;
}
