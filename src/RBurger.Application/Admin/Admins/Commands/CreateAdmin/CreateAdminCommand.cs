using MediatR;
using RBurger.Application.Admin.Admins.DTOs;

namespace RBurger.Application.Admin.Admins.Commands.CreateAdmin;

public class CreateAdminCommand : IRequest<AdminAdminResponse>
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}