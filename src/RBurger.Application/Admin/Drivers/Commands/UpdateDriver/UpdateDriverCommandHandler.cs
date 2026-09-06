using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Drivers.Commands.UpdateDriver;

public class UpdateDriverCommandHandler : IRequestHandler<UpdateDriverCommand, DriverAdminResponse>
{
    private readonly IDriverRepository _driverRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateDriverCommandHandler(
        IDriverRepository driverRepository,
        IBranchRepository branchRepository,
        IPasswordHasher passwordHasher)
    {
        _driverRepository = driverRepository;
        _branchRepository = branchRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<DriverAdminResponse> Handle(UpdateDriverCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "driver ... doesn't exist".
        var driver = await _driverRepository.GetByIdAsync(request.Id, cancellationToken);
        if (driver is null)
        {
            throw new NotFoundException($"Driver {request.Id} was not found.");
        }

        if (request.FullName is not null)
        {
            driver.FullName = request.FullName;
        }

        if (request.Vehicle is not null)
        {
            driver.Vehicle = request.Vehicle;
        }

        if (request.BranchId is not null)
        {
            // §7.8: 404 "branch ... doesn't exist" - re-validated here the same way
            // CreateDriverCommandHandler validates it on create, since a branch reassignment
            // must not silently accept a non-existent branchId.
            var branch = await _branchRepository.GetByIdAsync(request.BranchId.Value, cancellationToken);
            if (branch is null)
            {
                throw new NotFoundException($"Branch {request.BranchId.Value} was not found.");
            }

            driver.BranchId = request.BranchId.Value;
        }

        if (request.Password is not null)
        {
            // §7.6.3: "Changing the password here is the only way a driver's credential is
            // ever reset."
            driver.PasswordHash = _passwordHasher.Hash(request.Password);
        }

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
