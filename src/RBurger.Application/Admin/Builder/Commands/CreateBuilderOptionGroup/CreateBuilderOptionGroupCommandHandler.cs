using MediatR;
using RBurger.Application.Admin.Builder.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Admin.Builder.Commands.CreateBuilderOptionGroup;

public class CreateBuilderOptionGroupCommandHandler
    : IRequestHandler<CreateBuilderOptionGroupCommand, BuilderOptionGroupAdminResponse>
{
    private readonly IBuilderOptionGroupRepository _repository;

    public CreateBuilderOptionGroupCommandHandler(IBuilderOptionGroupRepository repository)
    {
        _repository = repository;
    }

    public async Task<BuilderOptionGroupAdminResponse> Handle(
        CreateBuilderOptionGroupCommand request, CancellationToken cancellationToken)
    {
        var group = new BuilderOptionGroup
        {
            GroupKey = request.GroupKey,
            IsSingleSelect = request.IsSingleSelect,
            Options = request.Options.Select(o => new BuilderOption
            {
                NameAr = o.NameAr,
                NameEn = o.NameEn,
                ExtraPrice = o.ExtraPrice
            }).ToList()
        };

        await _repository.AddAsync(group, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new BuilderOptionGroupAdminResponse
        {
            Id = group.Id,
            GroupKey = group.GroupKey,
            IsSingleSelect = group.IsSingleSelect,
            Options = group.Options.Select(o => new BuilderOptionAdminResponse
            {
                Id = o.Id,
                NameAr = o.NameAr,
                NameEn = o.NameEn,
                ExtraPrice = o.ExtraPrice
            }).ToList()
        };
    }
}