using MediatR;
using RBurger.Application.Admin.Admins.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;
using AdminEntity = RBurger.Domain.Entities.Admin;

namespace RBurger.Application.Admin.Admins.Commands.CreateAdmin;

public class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand, AdminAdminResponse>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateAdminCommandHandler(IAdminRepository adminRepository, IPasswordHasher passwordHasher)
    {
        _adminRepository = adminRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<AdminAdminResponse> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        var existingAdmin = await _adminRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingAdmin is not null)
        {
            throw new ConflictException("An admin with this username already exists.", "DUPLICATE_USERNAME");
        }

        var admin = new AdminEntity
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            FullName = request.FullName,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true
            // CreatedAt: DB default GETUTCDATE() (AdminConfiguration), same as Admin seed row.
        };

        await _adminRepository.AddAsync(admin, cancellationToken);
        await _adminRepository.SaveChangesAsync(cancellationToken);

        return new AdminAdminResponse
        {
            AdminId = admin.Id,
            Username = admin.Username,
            FullName = admin.FullName,
            IsActive = admin.IsActive
        };
    }
}