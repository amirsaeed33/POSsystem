using System.Text.Json.Serialization;

namespace SmartPos.Mobile.Models;

public class DashboardDto
{
    [JsonPropertyName("userDisplayName")]
    public string? UserDisplayName { get; set; }

    [JsonPropertyName("totalProducts")]
    public int TotalProducts { get; set; }

    [JsonPropertyName("inStockCount")]
    public int InStockCount { get; set; }

    [JsonPropertyName("lowStockCount")]
    public int LowStockCount { get; set; }

    [JsonPropertyName("outOfStockCount")]
    public int OutOfStockCount { get; set; }

    [JsonPropertyName("todaySales")]
    public decimal TodaySales { get; set; }

    [JsonPropertyName("todayPurchases")]
    public decimal TodayPurchases { get; set; }

    [JsonPropertyName("todayExpenses")]
    public decimal TodayExpenses { get; set; }

    [JsonPropertyName("todayProfit")]
    public decimal TodayProfit { get; set; }

    [JsonPropertyName("growthPercentage")]
    public decimal GrowthPercentage { get; set; }

    [JsonPropertyName("isGrowthPositive")]
    public bool IsGrowthPositive { get; set; }

    [JsonPropertyName("last7Days")]
    public List<DailyOverviewDto> Last7Days { get; set; } = new();

    [JsonPropertyName("quickActions")]
    public DashboardQuickActionCountsDto? QuickActions { get; set; }

    [JsonPropertyName("timeline")]
    public List<DashboardTimelineEventDto> Timeline { get; set; } = new();
}

public class DailyOverviewDto
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("dayLabel")]
    public string? DayLabel { get; set; }

    [JsonPropertyName("sales")]
    public decimal Sales { get; set; }

    [JsonPropertyName("purchases")]
    public decimal Purchases { get; set; }

    [JsonPropertyName("profit")]
    public decimal Profit { get; set; }
}

public class DashboardQuickActionCountsDto
{
    [JsonPropertyName("lowStockCount")]
    public int LowStockCount { get; set; }

    [JsonPropertyName("pendingOrdersCount")]
    public int PendingOrdersCount { get; set; }

    [JsonPropertyName("todaySalesCount")]
    public int TodaySalesCount { get; set; }

    [JsonPropertyName("branchCount")]
    public int BranchCount { get; set; }
}

public class DashboardTimelineEventDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }
}

public class OnlineOrderPipelineDto
{
    [JsonPropertyName("todayTotalOrders")]
    public int TodayTotalOrders { get; set; }

    [JsonPropertyName("pendingCount")]
    public int PendingCount { get; set; }

    [JsonPropertyName("approvedCount")]
    public int ApprovedCount { get; set; }

    [JsonPropertyName("rejectedCount")]
    public int RejectedCount { get; set; }

    [JsonPropertyName("inProgressCount")]
    public int InProgressCount { get; set; }

    [JsonPropertyName("deliveredCount")]
    public int DeliveredCount { get; set; }

    [JsonPropertyName("stages")]
    public List<OnlineOrderPipelineStageDto> Stages { get; set; } = new();
}

public class OnlineOrderPipelineStageDto
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusName")]
    public string? StatusName { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class AdminDayBarItem
{
    public string DayLabel { get; set; } = string.Empty;
    public double SalesBarHeight { get; set; } = 4;
    public double PurchasesBarHeight { get; set; } = 4;
    public string SalesText { get; set; } = string.Empty;
    public string PurchasesText { get; set; } = string.Empty;
}

public class AdminTimelineItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public Color Accent { get; set; } = Colors.Gray;
}
