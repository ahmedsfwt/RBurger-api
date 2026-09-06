using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Drivers.Commands.UpdateDriverStatus;

public class UpdateDriverStatusCommandHandler : IRequestHandler<UpdateDriverStatusCommand, DriverStatusResponse>
{
    private readonly IDriverRepository _driverRepository;

    public UpdateDriverStatusCommandHandler(IDriverRepository driverRepository)
    {
        _driverRepository = driverRepository;
    }

    public async Task<DriverStatusResponse> Handle(
        UpdateDriverStatusCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "driver ... doesn't exist".
        var driver = await _driverRepository.GetByIdAsync(request.Id, cancellationToken);
        if (driver is null)
        {
            throw new NotFoundException($"Driver {request.Id} was not found.");
        }

        driver.IsActive = request.IsActive;
        await _driverRepository.SaveChangesAsync(cancellationToken);

        return new DriverStatusResponse
        {
            DriverId = driver.Id,
            IsActive = driver.IsActive
        };
    }
}
