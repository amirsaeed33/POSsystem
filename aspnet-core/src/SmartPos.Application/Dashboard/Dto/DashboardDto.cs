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

        public List<DailyOverviewDto> Last7Days { get; set; }

        public List<DailyOverviewDto> Last15Days { get; set; }

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

        public int BranchCount { get; set; }
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

    public class DailyOverviewDto
    {
        public DateTime Date { get; set; }

        public string DayLabel { get; set; }

        public decimal Sales { get; set; }

        public decimal Purchases { get; set; }

        public decimal Expenses { get; set; }

        public decimal CostOfGoodsSold { get; set; }

        public decimal Profit { get; set; }
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

        /// <summary>Gross profit for the month: Sales − CostOfGoodsSold − Expenses.</summary>
        public decimal Profit { get; set; }
    }

    public class DashboardProductRowDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Sku { get; set; }

        public string CategoryName { get; set; }

        public string BrandName { get; set; }

        public string UnitName { get; set; }

        public decimal Units { get; set; }

        public decimal Price { get; set; }

        public decimal CostPrice { get; set; }

        public decimal ProfitPerUnit { get; set; }

        public string Status { get; set; }

        public string ImagePath { get; set; }
    }

    public class GetPinnedProductsInput
    {
        public List<int> ProductIds { get; set; } = new List<int>();
    }

    public class PinnedProductOverviewDto
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string Barcode { get; set; }

        public string CategoryName { get; set; }

        public string BrandName { get; set; }

        public string UnitName { get; set; }

        public string ImagePath { get; set; }

        public decimal TodayQuantitySold { get; set; }

        public decimal TodayRevenue { get; set; }

        public decimal TodayProfit { get; set; }

        public decimal CurrentStock { get; set; }

        public decimal AlertQuantityLimit { get; set; }
    }
    // ONLINE-PIPELINE START
    public class GetOnlineOrderPipelineInput
    {
        public int? BranchId { get; set; }

        public DateTime? Date { get; set; }
    }

    public class OnlineOrderPipelineStageDto
    {
        public int Status { get; set; }

        public string StatusName { get; set; }

        public int Count { get; set; }
    }

    public class OnlineOrderPipelineDto
    {
        /// <summary>
        /// Total online orders placed today within server-local date boundary [todayStart, tomorrow).
        /// </summary>
        public int TodayTotalOrders { get; set; }

        public int TodayOrdersCount => TodayTotalOrders;

        /// <summary>
        /// Pending online orders (CustomerOrderStatus.Pending = 0).
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// Approved/Accepted online orders (CustomerOrderStatus.Approved = 1).
        /// </summary>
        public int ApprovedCount { get; set; }

        public int AcceptedCount => ApprovedCount;

        /// <summary>
        /// Rejected online orders (CustomerOrderStatus.Rejected = 2).
        /// </summary>
        public int RejectedCount { get; set; }

        /// <summary>
        /// In-progress/Preparing online orders (CustomerOrderStatus.InProgress = 3).
        /// </summary>
        public int InProgressCount { get; set; }

        public int PreparingCount => InProgressCount;

        /// <summary>
        /// Delivered online orders (CustomerOrderStatus.Delivered = 4).
        /// </summary>
        public int DeliveredCount { get; set; }

        /// <summary>
        /// Pipeline stages breakdown for UI iteration.
        /// </summary>
        public List<OnlineOrderPipelineStageDto> Stages { get; set; } = new List<OnlineOrderPipelineStageDto>();
    }
    // ONLINE-PIPELINE END
}