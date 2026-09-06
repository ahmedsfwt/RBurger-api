using RBurger.Application.Branches.Queries.GetActiveBranches;
using RBurger.Application.Builder.Queries.GetBuilderOptions;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Menu.Queries.GetMenu;
using RBurger.Application.Tests.Admin;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.PublicEndpoints;

// Day 15 addition (Backend Parity Spec §3) - these three documented, public endpoints had no
// implementation at all before now.
public class PublicEndpointsTests
{
    [Fact]
    public async Task GetActiveBranches_returns_only_active_branches_ordered_by_id()
    {
        var branches = new FakeBranchRepository();
        branches.Branches.Add(new Branch { Id = 2, NameAr = "جرجا", NameEn = "Girga", IsActive = true });
        branches.Branches.Add(new Branch { Id = 1, NameAr = "سوهاج", NameEn = "Sohag", IsActive = true, EstimatedDeliveryTime = "25-35 mins" });
        branches.Branches.Add(new Branch { Id = 3, NameAr = "أسيوط", NameEn = "Assiut", IsActive = false });

        var handler = new GetActiveBranchesQueryHandler(branches);
        var result = await handler.Handle(new GetActiveBranchesQuery(), default);

        Assert.Equal(2, result.Count); // the inactive branch is excluded
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
        Assert.Equal("25-35 mins", result[0].EstimatedDeliveryTime);
    }

    [Fact]
    public async Task GetMenu_returns_categories_ordered_with_only_available_items_for_the_branch()
    {
        var branches = new FakeBranchRepository();
        branches.Branches.Add(new Branch { Id = 1, NameAr = "سوهاج", NameEn = "Sohag", IsActive = true });

        var categories = new FakeMenuCategoryRepository();
        categories.Categories.Add(new MenuCategory { Id = 2, Key = "sides", LabelAr = "أطباق جانبية", LabelEn = "Sides", SortOrder = 2 });
        categories.Categories.Add(new MenuCategory { Id = 1, Key = "burgers", LabelAr = "البرجر", LabelEn = "Burgers", SortOrder = 1 });

        var menuItems = new FakeMenuItemRepository();
        menuItems.MenuItems.Add(new MenuItem { Id = 101, CategoryId = 1, BranchId = 1, NameAr = "أ", NameEn = "Original", Price = 90, IsAvailable = true });
        menuItems.MenuItems.Add(new MenuItem { Id = 102, CategoryId = 1, BranchId = 1, NameAr = "ب", NameEn = "Unavailable", Price = 80, IsAvailable = false });
        menuItems.MenuItems.Add(new MenuItem { Id = 103, CategoryId = 1, BranchId = 2, NameAr = "ج", NameEn = "OtherBranch", Price = 70, IsAvailable = true });

        var handler = new GetMenuQueryHandler(branches, categories, menuItems);
        var result = await handler.Handle(new GetMenuQuery(1), default);

        Assert.Equal(2, result.Count);
        Assert.Equal("burgers", result[0].CategoryKey); // SortOrder 1 before "sides" (SortOrder 2)
        Assert.Single(result[0].Items); // unavailable + other-branch items excluded
        Assert.Equal(101, result[0].Items[0].Id);
        Assert.Empty(result[1].Items); // "sides" has no items for this branch, still included
    }

    [Fact]
    public async Task GetMenu_throws_NotFoundException_for_unknown_branch()
    {
        var handler = new GetMenuQueryHandler(
            new FakeBranchRepository(), new FakeMenuCategoryRepository(), new FakeMenuItemRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetMenuQuery(999), default));
    }

    [Fact]
    public async Task GetBuilderOptions_returns_groups_with_their_options()
    {
        var groups = new FakeBuilderOptionGroupRepository();
        var bunGroup = new BuilderOptionGroup { Id = 1, GroupKey = "bun", IsSingleSelect = true };
        bunGroup.Options.Add(new BuilderOption { Id = 1, NameAr = "بن كلاسيك", NameEn = "Classic Bun", ExtraPrice = 0, BuilderOptionGroupId = 1 });
        groups.Groups.Add(bunGroup);

        var handler = new GetBuilderOptionsQueryHandler(groups);
        var result = await handler.Handle(new GetBuilderOptionsQuery(), default);

        var group = Assert.Single(result);
        Assert.Equal("bun", group.GroupKey);
        Assert.True(group.IsSingleSelect);
        Assert.Single(group.Options);
        Assert.Equal("Classic Bun", group.Options[0].NameEn);
    }
}
