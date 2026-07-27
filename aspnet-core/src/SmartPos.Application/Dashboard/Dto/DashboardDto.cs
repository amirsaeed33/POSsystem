using System;
using System.Collections.Generic;

namespace SmartPos.Dashboard.Dto
{
    public class DashboardDto
    {
        public string UserDisplayName { get; set; }

        public string UserImageUrl { get; set; }

        public int TotalProducts { get; set; }

        public int InStockCount { get; set; }

        public int LowStockCount { get; set; }

        public int OutOfStockCount { get; set; }

        public decimal InStockUnits { get; set; }

        public decimal LowStockUnits { get; set; }

        public int LowStockThreshold { get; set; }

        public decimal TodaySales { get; set; }

        public decimal TodayPurchases { get; set; }

        public decimal TodayExpenses { get; set; }

        public decimal TodayProfit { get; set; }

        /// <summary>Average profit margin % for the current month-to-date period.</summary>
        public decimal AverageProfitMargin { get; set; }

        /// <summary>Average profit margin % for the previous equivalent month-to-date period.</summary>
        public decimal PreviousAverageProfitMargin { get; set; }

        /// <summary>((Current - Previous) / Previous) * 100. Safe when previous is zero.</summary>
        public decimal GrowthPercentage { get; set; }

        public bool IsGrowthPositive { get; set; }

        public List<MonthlyCashFlowDto> CashFlow { get; set; }

        public List<DashboardProductRowDto> Products { get; set; }

        /// <summary>Counts for dashboard Quick Actions badges.</summary>
        public DashboardQuickActionCountsDto QuickActions { get; set; }

        /// <summary>Latest customers or sales for the side list.</summary>
        public string LatestListTitle { get; set; }

        public List<DashboardLatestListItemDto> LatestListItems { get; set; }

        /// <summary>Merged recent activity for the timeline widget.</summary>
        public List<DashboardTimelineEventDto> Timeline { get; set; }
    }

    public class DashboardQuickActionCountsDto
    {
        public int LowStockCount { get; set; }

        public int PendingOrdersCount { get; set; }

        public int TodaySalesCount { get; set; }

        public int CompanyProfileCount { get; set; }
    }

    public class DashboardLatestListItemDto
    {
        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string Initials { get; set; }
    }

    public class DashboardTimelineEventDto
    {
        /// <summary>sale | purchase | expense | stock</summary>
        public string Type { get; set; }

        public string Title { get; set; }

        public decimal Amount { get; set; }

        /// <summary>Optional quantity label for stock adjustments.</summary>
        public string QuantityLabel { get; set; }

        public DateTime OccurredAt { get; set; }
    }

    public class MonthlyCashFlowDto
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public string MonthLabel { get; set; }

        public decimal Income { get; set; }

        public decimal Expense { get; set; }

        /// <summary>Purchase totals for the month (used with Expense for outgoing).</summary>
        public decimal Purchases { get; set; }
    }

    public class DashboardProductRowDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Sku { get; set; }

        public string CategoryName { get; set; }

        public string BrandName { get; set; }

        public decimal Units { get; set; }

        public decimal Price { get; set; }

        public decimal CostPrice { get; set; }

        public decimal ProfitPerUnit { get; set; }

        public string Status { get; set; }

        public string ImagePath { get; set; }
    }
}
