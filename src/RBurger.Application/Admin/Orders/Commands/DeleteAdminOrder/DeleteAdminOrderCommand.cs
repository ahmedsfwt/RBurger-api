using MediatR;

namespace RBurger.Application.Admin.Orders.Commands.DeleteAdminOrder;

public record DeleteAdminOrderCommand(Guid OrderId, Guid AdminId) : IRequest;
