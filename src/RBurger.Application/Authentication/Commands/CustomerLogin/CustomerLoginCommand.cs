using MediatR;
using RBurger.Application.Authentication.DTOs;

namespace RBurger.Application.Authentication.Commands.CustomerLogin;

// §7.1 POST /api/v1/auth/customer/login request body - fields match exactly.
public class CustomerLoginCommand : IRequest<AuthResponse>
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
