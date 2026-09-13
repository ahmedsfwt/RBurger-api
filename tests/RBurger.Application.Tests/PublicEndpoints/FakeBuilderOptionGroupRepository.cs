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

    public Task AddAsync(BuilderOptionGroup group, CancellationToken cancellationToken)
    {
        Groups.Add(group); 
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<BuilderOptionGroup?> GetByIdWithOptionsAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Groups.FirstOrDefault(g => g.Id == id));
    }

    public void Update(BuilderOptionGroup group) { } 

    public void Delete(BuilderOptionGroup group)
    {
        Groups.Remove(group);
    }

    public Task RemoveOptionsAsync(ICollection<BuilderOption> options, CancellationToken cancellationToken)
    {
        return Task.CompletedTask; 
    }

}
