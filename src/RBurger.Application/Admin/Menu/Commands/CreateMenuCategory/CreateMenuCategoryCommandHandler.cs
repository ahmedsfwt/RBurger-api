using MediatR;
using RBurger.Application.Admin.Menu.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Admin.Menu.Commands.CreateMenuCategory;

public class CreateMenuCategoryCommandHandler : IRequestHandler<CreateMenuCategoryCommand, MenuCategoryAdminResponse>
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;

    public CreateMenuCategoryCommandHandler(IMenuCategoryRepository menuCategoryRepository)
    {
        _menuCategoryRepository = menuCategoryRepository;
    }

    public async Task<MenuCategoryAdminResponse> Handle(CreateMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        var existing = await _menuCategoryRepository.GetByKeyAsync(request.CategoryKey, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"Menu category '{request.CategoryKey}' already exists.", "MENU_CATEGORY_ALREADY_EXISTS");
        }

        var category = new MenuCategory
        {
            Key = request.CategoryKey,
            LabelAr = request.LabelAr,
            LabelEn = request.LabelEn,
            SortOrder = request.SortOrder
        };

        await _menuCategoryRepository.AddAsync(category, cancellationToken);
        await _menuCategoryRepository.SaveChangesAsync(cancellationToken);

        return new MenuCategoryAdminResponse
        {
            Id = category.Id,
            CategoryKey = category.Key,
            LabelAr = category.LabelAr,
            LabelEn = category.LabelEn,
            SortOrder = category.SortOrder
        };
    }
}