namespace RBurger.Application.Admin.Admins.DTOs;

public class AdminAdminResponse
{
    public Guid AdminId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}