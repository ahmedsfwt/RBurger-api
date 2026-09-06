using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailResponse>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDetailResponse> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            // §7.8: 404 "Order/menu item/branch/driver id doesn't exist".
            throw new NotFoundException($"Order {request.OrderId} was not found.");
        }

        // §7.4 header row: "Customer JWT (own order) or Driver JWT (assigned order)".
        // §7.8: 403 "Valid JWT but wrong role or not the resource owner". Day 7 addition:
        // completes the previously-deferred Driver branch - a Customer must own the order,
        // a Driver must be its assigned driver; exactly one of the two requester ids is
        // populated by the calling controller, so this also still rejects a Customer JWT
        // requesting someone else's order and a Driver JWT requesting an unassigned order,
        // preserving all existing Day 4 Customer behavior unchanged.
        var isOwningCustomer = request.RequestingCustomerId is not null
            && order.CustomerId == request.RequestingCustomerId;
        var isAssignedDriver = request.RequestingDriverId is not null
            && order.DriverId == request.RequestingDriverId;

        if (!isOwningCustomer && !isAssignedDriver)
        {
            throw new ForbiddenException("You do not have access to this order.");
        }

        return new OrderDetailResponse
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Stage = order.Stage,
            Branch = new BranchSummaryDto
            {
                NameAr = order.Branch.NameAr,
                NameEn = order.Branch.NameEn,
                EstimatedDeliveryTime = order.Branch.EstimatedDeliveryTime // Day 14 (§1.4)
            },
            Items = order.OrderItems.Select(oi => new OrderItemSummaryDto
            {
                NameAr = oi.NameAr,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList(),
            Notes = order.Notes,
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            Total = order.Total,
            Payment = new PaymentSummaryDto
            {
                Method = order.Payment?.Method ?? order.PaymentMethod,
                Status = order.Payment?.Status ?? "pending"
            },
            CustomerReceivedAt = order.CustomerReceivedAt,
            // Day 8 approved decision #2: order.Review is now Include()'d by
            // GetByIdWithDetailsAsync, so an existing review is projected using the exact
            // same shape as POST .../review's response (CreateReviewResponse) instead of the
            // previous hard-coded null. Still null when order.Review is null (no review yet).
            Review = order.Review is null
                ? null
                : new CreateReviewResponse
                {
                    ReviewId = order.Review.Id,
                    OrderId = order.Review.OrderId,
                    Rating = order.Review.Rating,
                    Comment = order.Review.Comment,
                    CreatedAt = order.Review.CreatedAt
                },
            // Approved decision #6 (still open, Day 8 keeps it deferred): no documented
            // computation rule exists anywhere in the Technical & Product Documentation - left null.
            EtaSecondsRemaining = null
        };
    }
}
