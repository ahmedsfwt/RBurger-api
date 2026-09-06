using RBurger.Domain.Entities;

namespace RBurger.Application.Common.Interfaces;

// Day 15 addition (§7.3 GET /api/v1/builder/options - public endpoint). No prior repository
// existed for this table: CreateOrderCommandHandler stores a custom burger's client-supplied
// name/price directly (§7.4) without validating against BuilderOptions, so this is the first
// read path that needs it.
public interface IBuilderOptionGroupRepository
{
    // All groups with their options loaded - the full documented response shape in one call
    // (§7.3's example lists every group/option together, no pagination documented).
    Task<List<BuilderOptionGroup>> GetAllWithOptionsAsync(CancellationToken cancellationToken);
}
