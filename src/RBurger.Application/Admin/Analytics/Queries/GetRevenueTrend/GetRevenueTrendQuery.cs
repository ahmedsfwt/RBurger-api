using MediatR;
using RBurger.Application.Admin.Analytics.DTOs;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Analytics.Queries.GetRevenueTrend;

// §7.6.6 GET /api/v1/admin/analytics/revenue-trend?days=7. Day 14: implemented (Backend Parity
// Spec §1.1) - the only blocker (gross-order-total vs captured-payment-only revenue) is now
// resolved: revenue = SUM(Payments.Amount) WHERE Status="captured", excluding cancelled orders
// (§1.3). Days are trailing calendar days grouped by Order.CreatedAt's UTC date, the convention
// the route's own "days" query param already establishes.
public record GetRevenueTrendQuery(int Days) : IRequest<List<RevenueTrendPointDto>>, ICacheableQuery
{
    string ICacheableQuery.CacheKey => $"analytics:revenue-trend:{Days}";
    int ICacheableQuery.CacheDurationSeconds => 60;
}

public class GetRevenueTrendQueryHandler
    : IRequestHandler<GetRevenueTrendQuery, List<RevenueTrendPointDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetRevenueTrendQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<List<RevenueTrendPointDto>> Handle(
        GetRevenueTrendQuery request, CancellationToken cancellationToken)
    {
        var days = request.Days < 1 ? 7 : request.Days;

        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-(days - 1)); // trailing `days` calendar days, today inclusive
        var to = today.AddDays(1); // exclusive upper bound

        var revenueByDay = await _orderRepository.GetCapturedRevenueByDayAsync(from, to, cancellationToken);

        return Enumerable.Range(0, days)
            .Select(offset => from.AddDays(offset))
            .Select(day => new RevenueTrendPointDto
            {
                Date = day.ToString("yyyy-MM-dd"),
                Revenue = revenueByDay.GetValueOrDefault(day, 0m)
            })
            .ToList();
    }
}
