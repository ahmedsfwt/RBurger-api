using MediatR;
using RBurger.Application.Admin.Branches.DTOs;

namespace RBurger.Application.Admin.Branches.Queries.GetAdminBranches;

public class GetAdminBranchesQuery : IRequest<List<BranchAdminResponse>>
{
}