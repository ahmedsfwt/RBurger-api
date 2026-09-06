using MediatR;
using RBurger.Application.Admin.Analytics.DTOs;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Analytics.Queries.GetRatingDistribution;

public record GetRatingDistributionQuery : IRequest<List<RatingDistributionDto>>, ICacheableQuery
{
    string ICacheableQuery.CacheKey => "analytics:rating-distribution";
    int ICacheableQuery.CacheDurationSeconds => 60;
}

// §7.6.6: "Review counts grouped by star rating (1-5)." All 5 star values are always
// represented (including 0-count ones), mirroring the documented example and
// GetOrdersByStatusQueryHandler's identical "always show every bucket" convention.
public class GetRatingDistributionQueryHandler
    : IRequestHandler<GetRatingDistributionQuery, List<RatingDistributionDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetRatingDistributionQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<List<RatingDistributionDto>> Handle(
        GetRatingDistributionQuery request, CancellationToken cancellationToken)
    {
        var counts = await _reviewRepository.GetRatingDistributionAsync(cancellationToken);

        return Enumerable.Range(1, 5)
            .Select(stars => new RatingDistributionDto
            {
                Stars = stars,
                Count = counts.GetValueOrDefault((byte)stars, 0)
            })
            .ToList();
    }
}
