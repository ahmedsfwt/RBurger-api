using MediatR;

namespace RBurger.Application.Admin.Builder.Commands.DeleteBuilderOptionGroup;

public class DeleteBuilderOptionGroupCommand : IRequest
{
    public int Id { get; set; }
}