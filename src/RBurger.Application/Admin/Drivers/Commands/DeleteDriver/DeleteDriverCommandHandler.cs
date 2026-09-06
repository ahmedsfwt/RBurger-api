using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Drivers.Commands.DeleteDriver;

public class DeleteDriverCommandHandler : IRequestHandler<DeleteDriverCommand>
{
    private readonly IDriverRepository _driverRepository;
    private readonly IOrderRepository _orderRepository;

    public DeleteDriverCommandHandler(IDriverRepository driverRepository, IOrderRepository orderRepository)
    {
        _driverRepository = driverRepository;
        _orderRepository = orderRepository;
    }

    public async Task Handle(DeleteDriverCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "driver ... doesn't exist".
        var driver = await _driverRepository.GetByIdAsync(request.Id, cancellationToken);
        if (driver is null)
        {
            throw new NotFoundException($"Driver {request.Id} was not found.");
        }

        // §7.6.3: "Rejected with 422 if the driver has non-terminal (Stage 1-2) assigned
        // orders - reassign or wait for completion, or disable instead of deleting."
        var hasNonTerminalOrders = await _orderRepository.HasNonTerminalOrdersForDriverAsync(
            driver.Id, cancellationToken);
        if (hasNonTerminalOrders)
        {
            throw new UnprocessableEntityException(
                "Driver cannot be deleted because it has non-terminal assigned orders.",
                "DRIVER_HAS_NON_TERMINAL_ORDERS");
        }

        await _driverRepository.DeleteAsync(driver, cancellationToken);

        // Note: if the driver also has terminal (Delivered) order history, SaveChangesAsync's
        // real EF Core implementation converts the resulting FK-Restrict DbUpdateException
        // into a 422 (DRIVER_HAS_ORDER_HISTORY) instead of letting it surface as a 500 - see
        // DriverRepository.SaveChangesAsync's XML comment and the Day 11 report's discrepancy
        // section for why this is a separate, undocumented-but-necessary safeguard beyond the
        // non-terminal-orders check above.
        await _driverRepository.SaveChangesAsync(cancellationToken);
    }
}
