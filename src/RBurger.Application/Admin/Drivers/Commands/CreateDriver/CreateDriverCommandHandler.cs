using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Admin.Drivers.Commands.CreateDriver;

public class CreateDriverCommandHandler : IRequestHandler<CreateDriverCommand, DriverAdminResponse>
{
    private readonly IDriverRepository _driverRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;

    public CreateDriverCommandHandler(
        IDriverRepository driverRepository,
        IBranchRepository branchRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService)
    {
        _driverRepository = driverRepository;
        _branchRepository = branchRepository;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
    }

    public async Task<DriverAdminResponse> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "branch ... doesn't exist", mirroring CreateMenuItemCommandHandler's
        // identical branchId existence check (Day 10).
        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null)
        {
            throw new NotFoundException($"Branch {request.BranchId} was not found.");
        }

        // §6.2: Drivers.Phone is unique. §7.8: "409 Conflict ... duplicate phone on ...
        // admin driver creation", mirroring CustomerSignupCommandHandler's identical check.
        var existingDriver = await _driverRepository.GetByPhoneAsync(request.Phone, cancellationToken);
        if (existingDriver is not null)
        {
            throw new ConflictException("A driver with this phone number already exists.", "DUPLICATE_PHONE");
        }

        // §6.3: "the command handler behind POST /api/v1/admin/drivers ... always stamps
        // CreatedByAdminId." [Authorize(Roles = "Admin")] on the controller already guarantees
        // the caller is an authenticated Admin, so ICurrentUserService.AdminId is always
        // populated here - defensive null-check kept only for the same reason
        // OrdersController's [Authorize]-then-null-check pattern already does elsewhere.
        if (_currentUserService.AdminId is null)
        {
            throw new UnauthorizedAccessException("Admin identity could not be resolved from the JWT.");
        }

        var driver = new Driver
        {
            // Approved Day 2 decision #6 (client/application-generated GUIDs), mirroring
            // CustomerSignupCommandHandler's identical Id assignment.
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Vehicle = request.Vehicle,
            BranchId = request.BranchId,
            CreatedByAdminId = _currentUserService.AdminId.Value,
            IsActive = true // §6.2: "bit | default 1"
            // CreatedAt intentionally left unset: §6.2's documented DB default
            // (GETUTCDATE(), DriverConfiguration) populates it on insert.
        };

        await _driverRepository.AddAsync(driver, cancellationToken);
        await _driverRepository.SaveChangesAsync(cancellationToken);

        return new DriverAdminResponse
        {
            DriverId = driver.Id,
            FullName = driver.FullName,
            Phone = driver.Phone,
            Vehicle = driver.Vehicle,
            BranchId = driver.BranchId,
            IsActive = driver.IsActive
        };
    }
}
