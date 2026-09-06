using MediatR;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Builder.Queries.GetBuilderOptions;

// §7.3 GET /api/v1/builder/options response option entry:
// { "id":1, "nameAr":"بن كلاسيك", "nameEn":"Classic Bun", "extraPrice":0 }
public class BuilderOptionDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
}

// §7.3 response array entry: { "groupKey":"bun", "isSingleSelect":true, "options":[...] }
public class BuilderOptionGroupResponseDto
{
    public string GroupKey { get; set; } = string.Empty;
    public bool IsSingleSelect { get; set; }
    public List<BuilderOptionDto> Options { get; set; } = new();
}

// §7.3 GET /api/v1/builder/options - Public. Day 15 addition: this documented, public,
// customer-facing endpoint had no implementation at all before now.
public record GetBuilderOptionsQuery : IRequest<List<BuilderOptionGroupResponseDto>>;

public class GetBuilderOptionsQueryHandler
    : IRequestHandler<GetBuilderOptionsQuery, List<BuilderOptionGroupResponseDto>>
{
    private readonly IBuilderOptionGroupRepository _builderOptionGroupRepository;

    public GetBuilderOptionsQueryHandler(IBuilderOptionGroupRepository builderOptionGroupRepository)
    {
        _builderOptionGroupRepository = builderOptionGroupRepository;
    }

    public async Task<List<BuilderOptionGroupResponseDto>> Handle(
        GetBuilderOptionsQuery request, CancellationToken cancellationToken)
    {
        var groups = await _builderOptionGroupRepository.GetAllWithOptionsAsync(cancellationToken);

        return groups.Select(g => new BuilderOptionGroupResponseDto
        {
            GroupKey = g.GroupKey,
            IsSingleSelect = g.IsSingleSelect,
            Options = g.Options.Select(o => new BuilderOptionDto
            {
                Id = o.Id,
                NameAr = o.NameAr,
                NameEn = o.NameEn,
                ExtraPrice = o.ExtraPrice
            }).ToList()
        }).ToList();
    }
}
