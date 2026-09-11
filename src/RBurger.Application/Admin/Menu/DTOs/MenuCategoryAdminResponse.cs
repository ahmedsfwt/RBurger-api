namespace RBurger.Application.Admin.Menu.DTOs;

public class MenuCategoryAdminResponse
{
    public int Id { get; set; }
    public string CategoryKey { get; set; } = string.Empty;
    public string LabelAr { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}