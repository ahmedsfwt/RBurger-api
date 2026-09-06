using MediatR;
using RBurger.Application.Admin.Analytics.DTOs;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Analytics.Queries.GetTopItems;

// §7.6.6: "GET /api/v1/admin/analytics/top-items?limit=5 ... Best-selling menu items by order
// count", response field "unitsSold". Limit param default is 5 per the documented route
// example (defaulted in the controller, mirroring §7.0's page/pageSize default pattern).
public record GetTopItemsQuery(int Limit) : IRequest<List<TopSellingItemDto>>, ICacheableQuery
{
    string ICacheableQuery.CacheKey => $"analytics:top-items:{Limit}";
    int ICacheableQuery.CacheDurationSeconds => 60;
}

public class GetTopItemsQueryHandler : IRequestHandler<GetTopItemsQuery, List<TopSellingItemDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuItemRepository _menuItemRepository;

    public GetTopItemsQueryHandler(
        IOrderRepository orderRepository, IMenuItemRepository menuItemRepository)
    {
        _orderRepository = orderRepository;
        _menuItemRepository = menuItemRepository;
    }

    public async Task<List<TopSellingItemDto>> Handle(
        GetTopItemsQuery request, CancellationToken cancellationToken)
    {
        var limit = request.Limit < 1 ? 5 : request.Limit;

        var topSelling = await _orderRepository.GetTopSellingMenuItemIdsAsync(limit, cancellationToken);
        var menuItems = await _menuItemRepository.GetByIdsAsync(
            topSelling.Select(x => x.MenuItemId), cancellationToken);
        var menuItemsById = menuItems.ToDictionary(m => m.Id);

        // A menu item could have been deleted after being ordered (§7.6.1: OrderItems keep
        // their name/price snapshot, but the MenuItems row itself is gone) - such ids are
        // skipped rather than shown with an invented/blank name, since §7.6.6's response
        // shape requires nameAr/nameEn per item.
        return topSelling
            .Where(x => menuItemsById.ContainsKey(x.MenuItemId))
            .Select(x => new TopSellingItemDto
            {
                MenuItemId = x.MenuItemId,
                NameAr = menuItemsById[x.MenuItemId].NameAr,
                NameEn = menuItemsById[x.MenuItemId].NameEn,
                UnitsSold = x.UnitsSold
            })
            .ToList();
    }
}
