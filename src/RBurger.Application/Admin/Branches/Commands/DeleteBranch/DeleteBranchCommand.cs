using MediatR;

namespace RBurger.Application.Admin.Branches.Commands.DeleteBranch;

// §7.6.2 DELETE /api/v1/admin/branches/{id}.
public class DeleteBranchCommand : IRequest
{
    public int Id { get; set; }
}
