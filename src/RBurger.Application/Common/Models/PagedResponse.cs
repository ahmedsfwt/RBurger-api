namespace RBurger.Application.Common.Models;

// §7.0: "Pagination (list endpoints): query params page (default 1) and pageSize (default 20);
// response wraps items in { items: [...], page, pageSize, totalCount }."
// Generic so every paginated §7 list endpoint (starting with GET /api/v1/orders/mine) reuses
// the same envelope shape instead of a bespoke class per endpoint.
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
