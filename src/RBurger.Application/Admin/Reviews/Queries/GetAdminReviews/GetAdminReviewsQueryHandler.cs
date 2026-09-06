using MediatR;
using RBurger.Application.Admin.Reviews.DTOs;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Common.Models;

namespace RBurger.Application.Admin.Reviews.Queries.GetAdminReviews;

public class GetAdminReviewsQueryHandler
    : IRequestHandler<GetAdminReviewsQuery, PagedResponse<AdminReviewListItemDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetAdminReviewsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<PagedResponse<AdminReviewListItemDto>> Handle(
        GetAdminReviewsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var (reviews, totalCount) = await _reviewRepository.GetPagedForAdminAsync(
            page, pageSize, cancellationToken);

        var items = reviews.Select(r => new AdminReviewListItemDto
        {
            ReviewId = r.Id,
            OrderNumber = r.Order.OrderNumber,
            CustomerName = r.Order.CustomerName,
            Rating = r.Rating,
            Comment = r.Comment
        }).ToList();

        return new PagedResponse<AdminReviewListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
