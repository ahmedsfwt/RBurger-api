using MediatR;

namespace RBurger.Application.Admin.Reviews.Commands.DeleteAdminReview;

public record DeleteAdminReviewCommand(Guid ReviewId) : IRequest;
