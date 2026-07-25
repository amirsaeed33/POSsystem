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

        public List<MonthlyCashFlowDto> CashFlow { get; set; }

        public List<DashboardProductRowDto> Products { get; set; }
    }

    public class MonthlyCashFlowDto
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public string MonthLabel { get; set; }

        public decimal Income { get; set; }

        public decimal Expense { get; set; }
    }

    public class DashboardProductRowDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Sku { get; set; }

        public string CategoryName { get; set; }

        public string BrandName { get; set; }

        public decimal Units { get; set; }

        public string Status { get; set; }

        public string ImagePath { get; set; }
    }
}
