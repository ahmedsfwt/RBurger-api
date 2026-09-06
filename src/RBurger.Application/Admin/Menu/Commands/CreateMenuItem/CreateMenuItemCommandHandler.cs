using MediatR;
using RBurger.Application.Admin.Menu.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Admin.Menu.Commands.CreateMenuItem;

public class CreateMenuItemCommandHandler : IRequestHandler<CreateMenuItemCommand, MenuItemAdminResponse>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IBranchRepository _branchRepository;

    public CreateMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuCategoryRepository menuCategoryRepository,
        IBranchRepository branchRepository)
    {
        _menuItemRepository = menuItemRepository;
        _menuCategoryRepository = menuCategoryRepository;
        _branchRepository = branchRepository;
    }

    public async Task<MenuItemAdminResponse> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        // §7.8: 404 "menu item/branch/driver id doesn't exist" - extended here to categoryKey/
        // branchId on create, mirroring CreateOrderCommandHandler's existing 404 pattern for
        // referenced entities that don't exist.
        var category = await _menuCategoryRepository.GetByKeyAsync(request.CategoryKey, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException($"Menu category '{request.CategoryKey}' was not found.");
        }

        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null)
        {
            throw new NotFoundException($"Branch {request.BranchId} was not found.");
        }

        // §7.6.1: "a new item is always created without a photo ... until the dedicated
        // upload endpoint below is called." ImageUrl/ImageObjectKey/ImageUploadedAt all start
        // null - never accepted from this request (§6.2's binding rule).
        var menuItem = new MenuItem
        {
            CategoryId = category.Id,
            BranchId = request.BranchId,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            DescriptionAr = request.DescriptionAr,
            DescriptionEn = request.DescriptionEn,
            Price = request.Price,
            ImageUrl = null,
            ImageObjectKey = null,
            ImageUploadedAt = null,
            IsAvailable = true // §6.2: "bit | default 1"
        };

        await _menuItemRepository.AddAsync(menuItem, cancellationToken);
        await _menuItemRepository.SaveChangesAsync(cancellationToken);

        return new MenuItemAdminResponse
        {
            Id = menuItem.Id,
            CategoryKey = category.Key,
            NameAr = menuItem.NameAr,
            NameEn = menuItem.NameEn,
            DescriptionAr = menuItem.DescriptionAr,
            DescriptionEn = menuItem.DescriptionEn,
            Price = menuItem.Price,
            ImageUrl = menuItem.ImageUrl,
            IsAvailable = menuItem.IsAvailable
        };
    }
}
