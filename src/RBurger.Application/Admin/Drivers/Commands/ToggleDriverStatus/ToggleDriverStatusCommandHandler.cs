using MediatR;
using RBurger.Application.Admin.Drivers.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Drivers.Commands.ToggleDriverStatus;

public class ToggleDriverStatusCommandHandler
    : IRequestHandler<ToggleDriverStatusCommand, DriverStatusResponse>
{
    private readonly IDriverRepository _driverRepository;

    public ToggleDriverStatusCommandHandler(IDriverRepository driverRepository)
    {
        _driverRepository = driverRepository;
    }

    public async Task<DriverStatusResponse> Handle(
        ToggleDriverStatusCommand request, CancellationToken cancellationToken)
    {
        var driver = await _driverRepository.GetByIdAsync(request.Id, cancellationToken);
        if (driver is null)
        {
            throw new NotFoundException($"Driver {request.Id} was not found.");
        }

        driver.IsActive = !driver.IsActive;
        await _driverRepository.SaveChangesAsync(cancellationToken);

        return new DriverStatusResponse
        {
            DriverId = driver.Id,
            IsActive = driver.IsActive
        };
    }
}
