namespace RBurger.Application.Menu.DTOs;

// §7.3 GET /api/v1/menu?branchId= response item's nested "items" entries:
// { "id":101, "nameAr":"...", "nameEn":"...", "descriptionAr":"...", "descriptionEn":"...", "price":90, "imageUrl":null }
public class MenuItemSummaryDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
}

// §7.3 GET /api/v1/menu?branchId= response array entry:
// { "categoryKey":"burgers", "labelAr":"البرجر", "labelEn":"Burgers", "items":[...] }
public class MenuCategoryResponseDto
{
    public string CategoryKey { get; set; } = string.Empty;
    public string LabelAr { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public List<MenuItemSummaryDto> Items { get; set; } = new();
}
