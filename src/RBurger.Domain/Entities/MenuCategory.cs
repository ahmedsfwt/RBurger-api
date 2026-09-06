namespace RBurger.Domain.Entities;

public class MenuCategory
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string LabelAr { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    // §6.1 navigation property
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
