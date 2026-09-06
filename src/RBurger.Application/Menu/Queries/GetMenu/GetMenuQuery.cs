using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Menu.DTOs;

namespace RBurger.Application.Menu.Queries.GetMenu;

// §7.3 GET /api/v1/menu?branchId=1 - Public. Day 15 addition: this documented, public,
// customer-facing endpoint had no implementation at all before now.
public record GetMenuQuery(int BranchId) : IRequest<List<MenuCategoryResponseDto>>;

public class GetMenuQueryHandler : IRequestHandler<GetMenuQuery, List<MenuCategoryResponseDto>>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IMenuItemRepository _menuItemRepository;

    public GetMenuQueryHandler(
        IBranchRepository branchRepository,
        IMenuCategoryRepository menuCategoryRepository,
        IMenuItemRepository menuItemRepository)
    {
        _branchRepository = branchRepository;
        _menuCategoryRepository = menuCategoryRepository;
        _menuItemRepository = menuItemRepository;
    }

    public async Task<List<MenuCategoryResponseDto>> Handle(
        GetMenuQuery request, CancellationToken cancellationToken)
    {
        // §7.8's general "branch... doesn't exist" 404 pattern, applied here for consistency -
        // not explicitly itemized for this specific route in §7.8, but this is the same
        // established convention used for every other endpoint that takes a branch/entity id.
        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null)
        {
            throw new NotFoundException($"Branch {request.BranchId} was not found.");
        }

        var categories = await _menuCategoryRepository.GetAllOrderedAsync(cancellationToken);

        // IsAvailable filter (§7.6.1/Day 14's toggle) - see IMenuItemRepository's XML comment.
        var items = await _menuItemRepository.GetAvailableByBranchIdAsync(request.BranchId, cancellationToken);
        var itemsByCategory = items.ToLookup(i => i.CategoryId);

        // Categories with no items for this branch are still included (empty "items" array),
        // mirroring this project's existing "include all buckets, even zero-count ones"
        // convention (e.g. orders-by-status listing every Stage) rather than silently hiding
        // an entire category tab from the client.
        return categories.Select(c => new MenuCategoryResponseDto
        {
            CategoryKey = c.Key,
            LabelAr = c.LabelAr,
            LabelEn = c.LabelEn,
            Items = itemsByCategory[c.Id].Select(i => new MenuItemSummaryDto
            {
                Id = i.Id,
                NameAr = i.NameAr,
                NameEn = i.NameEn,
                DescriptionAr = i.DescriptionAr,
                DescriptionEn = i.DescriptionEn,
                Price = i.Price,
                ImageUrl = i.ImageUrl
            }).ToList()
        }).ToList();
    }
}
