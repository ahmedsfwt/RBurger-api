using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Tests.PublicEndpoints;

// Day 15 addition (§7.3 - public endpoint).
internal class FakeBuilderOptionGroupRepository : IBuilderOptionGroupRepository
{
    public List<BuilderOptionGroup> Groups { get; } = new();

    public Task<List<BuilderOptionGroup>> GetAllWithOptionsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Groups.ToList());
    }
}
