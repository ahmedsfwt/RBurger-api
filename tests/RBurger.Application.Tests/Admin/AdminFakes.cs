using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Tests.Admin;

// Day 11 addition - simple in-memory fake for ICurrentUserService, needed by
// CreateDriverCommandHandlerTests to supply the acting Admin's id (§6.3's "always stamps
// CreatedByAdminId"). No such fake existed prior to Day 11 since no Application handler
// consumed ICurrentUserService before now - see GetCustomerMeQuery's comment, which resolves
// the equivalent CustomerId in the controller instead and passes it as a plain query field.
internal class FakeCurrentUserService : ICurrentUserService
{
    public Guid? CustomerId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? AdminId { get; set; }
}

// Day 10 addition - simple in-memory fakes, following the exact same dependency-free pattern
// as tests/RBurger.Application.Tests/Orders/FakeRepositories.cs and
// tests/RBurger.Application.Tests/Authentication/FakeAuthDependencies.cs.
internal class FakeMenuCategoryRepository : IMenuCategoryRepository
{
    public List<MenuCategory> Categories { get; } = new();

    public Task<MenuCategory?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return Task.FromResult(Categories.FirstOrDefault(c => c.Key == key));
    }

    public Task<MenuCategory?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Categories.FirstOrDefault(c => c.Id == id));
    }

    // Day 15 addition (§7.3 - public endpoint).
    public Task<List<MenuCategory>> GetAllOrderedAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Categories.OrderBy(c => c.SortOrder).ToList());
    }

    public Task AddAsync(MenuCategory category, CancellationToken cancellationToken)
    {
        Categories.Add(category);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

// Fakes the Day 10 scaffold-only image storage contract. Two modes: the default
// (AlwaysThrow = true) mirrors NotConfiguredMenuItemImageStorage's real Infrastructure
// behavior for handler tests that need to assert the "not configured" failure path;
// AlwaysThrow = false lets other tests assert the surrounding orchestration (menu item
// lookup, field updates, SaveChanges) as if a real provider existed, without coupling those
// assertions to Infrastructure specifics.
internal class FakeMenuItemImageStorage : IMenuItemImageStorage
{
    public bool AlwaysThrow { get; set; } = true;
    public List<string> DeletedObjectKeys { get; } = new();

    public Task<MenuItemImageUploadResult> UploadAsync(
        int menuItemId,
        Stream content,
        string contentType,
        string? existingObjectKeyToReplace,
        CancellationToken cancellationToken)
    {
        if (AlwaysThrow)
        {
            throw new StorageNotConfiguredException();
        }

        if (existingObjectKeyToReplace is not null)
        {
            DeletedObjectKeys.Add(existingObjectKeyToReplace);
        }

        return Task.FromResult(new MenuItemImageUploadResult(
            $"https://media.rburger.app/menu-items/{menuItemId}/fake.jpg",
            $"menu-items/{menuItemId}/fake.jpg"));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (AlwaysThrow)
        {
            throw new StorageNotConfiguredException();
        }

        DeletedObjectKeys.Add(objectKey);
        return Task.CompletedTask;
    }
}
