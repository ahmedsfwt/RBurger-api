using MediatR;
using RBurger.Application.Admin.Reviews.DTOs;
using RBurger.Application.Common.Models;

namespace RBurger.Application.Admin.Reviews.Queries.GetAdminReviews;

public record GetAdminReviewsQuery(int Page, int PageSize) : IRequest<PagedResponse<AdminReviewListItemDto>>;
