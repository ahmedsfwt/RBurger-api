using MediatR;
using RBurger.Application.Authentication.DTOs;

namespace RBurger.Application.Authentication.Commands.DriverLogin;

// §7.2 POST /api/v1/auth/driver/login request body - fields match exactly, same shape as
// CustomerLoginCommand.
public class DriverLoginCommand : IRequest<DriverAuthResponse>
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
