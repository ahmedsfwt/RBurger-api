using MediatR;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Builder.Commands.DeleteBuilderOptionGroup;

public class DeleteBuilderOptionGroupCommandHandler : IRequestHandler<DeleteBuilderOptionGroupCommand>
{
    private readonly IBuilderOptionGroupRepository _repository;

    public DeleteBuilderOptionGroupCommandHandler(IBuilderOptionGroupRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteBuilderOptionGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _repository.GetByIdWithOptionsAsync(request.Id, cancellationToken);
        if (group is null)
        {
            throw new NotFoundException($"Builder option group {request.Id} was not found.");
        }

        await _repository.RemoveOptionsAsync(group.Options, cancellationToken);
        _repository.Delete(group);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}