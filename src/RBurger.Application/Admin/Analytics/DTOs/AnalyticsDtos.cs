namespace RBurger.Application.Admin.Analytics.DTOs;

// §7.6.6 GET /api/v1/admin/analytics/orders-by-status response example:
// [ { "stage":0,"count":3 }, ... ].
public class OrderStatusCountDto
{
    public int Stage { get; set; }
    public int Count { get; set; }
}

// §7.6.6 GET /api/v1/admin/analytics/orders-by-branch response example:
// [ { "branchId":1,"nameAr":"سوهاج","nameEn":"Sohag","count":18 }, ... ].
public class BranchOrderCountDto
{
    public int BranchId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int Count { get; set; }
}

// §7.6.6 GET /api/v1/admin/analytics/top-items response example:
// [ { "menuItemId":101,"nameAr":"أورجينال","nameEn":"Original","unitsSold":142 } ].
public class TopSellingItemDto
{
    public int MenuItemId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
}

// §7.6.6 GET /api/v1/admin/analytics/driver-performance response example:
// [ { "driverId":"d91a...","fullName":"Karim Adel","deliveriesCompleted":38 } ].
public class DriverPerformanceDto
{
    public Guid DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int DeliveriesCompleted { get; set; }
}

// §7.6.6 GET /api/v1/admin/analytics/rating-distribution response example:
// [ {"stars":1,"count":2}, ... ].
public class RatingDistributionDto
{
    public int Stars { get; set; }
    public int Count { get; set; }
}

// §7.6.6 GET /api/v1/admin/analytics/revenue-trend response example:
// [ { "date":"2026-07-24", "revenue":1620 }, ... ]. Day 14: implemented now that the revenue
// basis is resolved (Backend Parity Spec §1.1 - captured payments only).
public class RevenueTrendPointDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

// §7.6.6 GET /api/v1/admin/analytics/overview response:
// { totalRevenue, totalOrders, avgOrderValue, completionRatePercent, deltas: {...} }.
// Day 15 (Backend Parity Spec §11): the documented shape is fully preserved, but
// completionRatePercent/deltas.completionRate are nullable and returned as null - their
// formula remains genuinely undefined in Documentation v1.2, and no approved spec has defined
// one. "Isolate ONLY that field rather than returning 501 for the entire endpoint" (Backend
// Parity Spec §11) - see GetAnalyticsOverviewQueryHandler's XML comment.
public class AnalyticsOverviewDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AvgOrderValue { get; set; }
    public decimal? CompletionRatePercent { get; set; }
    public AnalyticsOverviewDeltasDto Deltas { get; set; } = new();
}

public class AnalyticsOverviewDeltasDto
{
    public decimal Revenue { get; set; }
    public decimal Orders { get; set; }
    public decimal AvgOrderValue { get; set; }
    public decimal? CompletionRate { get; set; }
}
