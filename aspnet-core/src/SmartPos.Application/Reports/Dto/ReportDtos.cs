using System;
using System.Collections.Generic;

namespace SmartPos.Reports.Dto
{
    public class ReportDateRangeInput
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string Keyword { get; set; }
    }

    public class SaleReportDto
    {
        public decimal TotalAmount { get; set; }

        public List<SaleReportRowDto> Items { get; set; }
    }

    public class SaleReportRowDto
    {
        public int Id { get; set; }

        public string InvoiceNo { get; set; }

        public DateTime SaleDate { get; set; }

        public string CustomerName { get; set; }

        public decimal TotalAmount { get; set; }

        public string Notes { get; set; }
    }

    public class PurchaseReportDto
    {
        public decimal TotalAmount { get; set; }

        public List<PurchaseReportRowDto> Items { get; set; }
    }

    public class PurchaseReportRowDto
    {
        public int Id { get; set; }

        public string InvoiceNo { get; set; }

        public DateTime PurchaseDate { get; set; }

        public string SupplierName { get; set; }

        public decimal TotalAmount { get; set; }

        public string Notes { get; set; }
    }

    public class ExpenseReportDto
    {
        public decimal TotalAmount { get; set; }

        public List<ExpenseReportRowDto> Items { get; set; }
    }

    public class ExpenseReportRowDto
    {
        public int Id { get; set; }

        public DateTime ExpenseDate { get; set; }

        public string ReferenceNo { get; set; }

        public string Description { get; set; }

        public string PaymentAccountName { get; set; }

        public decimal Amount { get; set; }
    }

    public class StockReportDto
    {
        public int TotalProducts { get; set; }

        public int InStockCount { get; set; }

        public int LowStockCount { get; set; }

        public int OutOfStockCount { get; set; }

        public decimal TotalStockUnits { get; set; }

        public decimal TotalStockCostValue { get; set; }

        public decimal TotalStockSellValue { get; set; }

        public decimal TotalStockProfit { get; set; }

        public List<StockReportRowDto> Items { get; set; }
    }

    public class StockReportRowDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Barcode { get; set; }

        public string CategoryName { get; set; }

        public string BrandName { get; set; }

        public string UnitName { get; set; }

        public decimal Price { get; set; }

        public decimal CostPrice { get; set; }

        public decimal ProfitPerUnit { get; set; }

        public decimal? ProfitMarginPercent { get; set; }

        public decimal StockProfit { get; set; }

        public decimal StockQuantity { get; set; }

        public decimal AlertQuantityLimit { get; set; }

        public string Status { get; set; }
    }
}
