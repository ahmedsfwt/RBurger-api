using MediatR;
using RBurger.Application.Admin.Builder.DTOs;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Application.Admin.Builder.Commands.UpdateBuilderOptionGroup;

public class UpdateBuilderOptionGroupCommandHandler
    : IRequestHandler<UpdateBuilderOptionGroupCommand, BuilderOptionGroupAdminResponse>
{
    private readonly IBuilderOptionGroupRepository _repository;

    public UpdateBuilderOptionGroupCommandHandler(IBuilderOptionGroupRepository repository)
    {
        _repository = repository;
    }

    public async Task<BuilderOptionGroupAdminResponse> Handle(
        UpdateBuilderOptionGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _repository.GetByIdWithOptionsAsync(request.Id, cancellationToken);
        if (group is null)
        {
            throw new NotFoundException($"Builder option group {request.Id} was not found.");
        }

        group.GroupKey = request.GroupKey;
        group.IsSingleSelect = request.IsSingleSelect;

        // استبدال كامل للخيارات القديمة بالجديدة.
        await _repository.RemoveOptionsAsync(group.Options, cancellationToken);
        group.Options.Clear();
        foreach (var option in request.Options)
        {
            group.Options.Add(new BuilderOption
            {
                NameAr = option.NameAr,
                NameEn = option.NameEn,
                ExtraPrice = option.ExtraPrice
            });
        }

        _repository.Update(group);
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