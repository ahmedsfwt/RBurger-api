using MediatR;
using RBurger.Application.Admin.Builder.DTOs;

namespace RBurger.Application.Admin.Builder.Commands.UpdateBuilderOptionGroup;

public class UpdateBuilderOptionGroupCommand : IRequest<BuilderOptionGroupAdminResponse>
{
    public int Id { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public bool IsSingleSelect { get; set; }
    public List<UpdateBuilderOptionItem> Options { get; set; } = new();
}

public class UpdateBuilderOptionItem
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
}