using MediatR;
using RBurger.Application.Admin.Builder.DTOs;

namespace RBurger.Application.Admin.Builder.Commands.CreateBuilderOptionGroup;

public class CreateBuilderOptionGroupCommand : IRequest<BuilderOptionGroupAdminResponse>
{
    public string GroupKey { get; set; } = string.Empty;
    public bool IsSingleSelect { get; set; }
    public List<CreateBuilderOptionItem> Options { get; set; } = new();
}

public class CreateBuilderOptionItem
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
}