using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Reviews.Commands.DeleteAdminReview;

// §7.6.6 DELETE /api/v1/admin/reviews/{id}: "Delete a review (e.g. abusive content)." No
// documented side effects beyond the row deletion itself (unlike menu items, deleting a review
// does not touch any other documented entity - Orders.hasReview is derived from Review's
// existence at read time, not a persisted flag).
public class DeleteAdminReviewCommandHandler : IRequestHandler<DeleteAdminReviewCommand>
{
    private readonly IReviewRepository _reviewRepository;

    public DeleteAdminReviewCommandHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task Handle(DeleteAdminReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
        {
            throw new NotFoundException($"Review {request.ReviewId} was not found.");
        }

        await _reviewRepository.DeleteAsync(review, cancellationToken);
        await _reviewRepository.SaveChangesAsync(cancellationToken);
    }
}
