namespace RBurger.Application.Admin.Builder.DTOs;

public class BuilderOptionGroupAdminResponse
{
    public int Id { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public bool IsSingleSelect { get; set; }
    public List<BuilderOptionAdminResponse> Options { get; set; } = new();
}

public class BuilderOptionAdminResponse
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
}