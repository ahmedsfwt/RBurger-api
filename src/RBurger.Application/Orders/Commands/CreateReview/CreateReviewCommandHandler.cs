using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;
using RBurger.Domain.Entities;

namespace RBurger.Application.Orders.Commands.CreateReview;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, CreateReviewResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IReviewRepository _reviewRepository;

    public CreateReviewCommandHandler(IOrderRepository orderRepository, IReviewRepository reviewRepository)
    {
        _orderRepository = orderRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<CreateReviewResponse> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            // §7.8: 404 "Order/menu item/branch/driver id doesn't exist".
            throw new NotFoundException($"Order {request.OrderId} was not found.");
        }

        // §7.4 header row: "Customer JWT (must own the order; ...)".
        // §7.8: 403 "Valid JWT but wrong role or not the resource owner".
        if (order.CustomerId != request.CustomerId)
        {
            throw new ForbiddenException("You do not have access to this order.");
        }

        // §7.4 header row + §6.3: "A Review can only be created once CustomerReceivedAt is not
        // null". Undocumented status code for this specific precondition (flagged in Phase 0
        // report) - 422/UnprocessableEntityException is an implementation decision, approved
        // for Day 5, mirroring the same pattern used for customer-received's Stage check.
        if (order.CustomerReceivedAt is null)
        {
            throw new UnprocessableEntityException(
                "A review can only be submitted after the order has been marked as received.",
                "ORDER_NOT_RECEIVED");
        }

        // §6.3: "only once per order (unique index on OrderId)". §7.8: 409 "duplicate review"
        // (literal). Fast-path pre-check; the DB unique index (ReviewConfiguration) remains
        // the final safety net for a race between two concurrent requests, translated by
        // ReviewRepository.SaveChangesAsync.
        var alreadyReviewed = await _reviewRepository.ExistsByOrderIdAsync(request.OrderId, cancellationToken);
        if (alreadyReviewed)
        {
            throw new ConflictException("A review already exists for this order.", "DUPLICATE_REVIEW");
        }

        var review = new Review
        {
            // Approved decision #6 (Day 1/2): client/application-generated GUID
            // (ValueGeneratedNever), consistent with every other entity in this codebase.
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            CustomerId = request.CustomerId,
            Rating = (byte)request.Rating,
            Comment = request.Comment
            // CreatedAt intentionally left unset - §6.2's documented DB default
            // (GETUTCDATE(), configured in ReviewConfiguration) populates it on insert,
            // exactly matching Order.CreatedAt's pattern in CreateOrderCommandHandler.
        };

        await _reviewRepository.AddAsync(review, cancellationToken);
        await _reviewRepository.SaveChangesAsync(cancellationToken);

        return new CreateReviewResponse
        {
            ReviewId = review.Id,
            OrderId = review.OrderId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }
}
