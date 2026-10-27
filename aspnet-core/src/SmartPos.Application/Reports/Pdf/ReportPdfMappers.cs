using System;
using System.Collections.Generic;
using System.Linq;
using SmartPos.Reports.Dto;

namespace SmartPos.Reports.Pdf
{
    public static class ReportPdfMappers
    {
        private static string FormatDateRange(ReportDateRangeInput input)
        {
            if (input == null) return "All Time";
            var from = input.FromDate?.ToString("yyyy-MM-dd");
            var to = input.ToDate?.ToString("yyyy-MM-dd");
            if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to)) return $"{from} to {to}";
            if (!string.IsNullOrEmpty(from)) return $"From {from}";
            if (!string.IsNullOrEmpty(to)) return $"Up to {to}";
            return "All Time";
        }

        public static ReportPdfModel ToPdfModel(this StockReportDto dto, ReportDateRangeInput input, string branchName = null)
        {
            var model = new ReportPdfModel
            {
                Title = "Stock Report",
                GeneratedAt = DateTime.Now,
                DateRange = FormatDateRange(input),
                BranchName = branchName,
                SearchKeyword = input?.Keyword
            };

            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Products", dto.TotalProducts.ToString("N0")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("In Stock", dto.InStockCount.ToString("N0")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Low Stock", dto.LowStockCount.ToString("N0")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Out of Stock", dto.OutOfStockCount.ToString("N0")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Stock Cost Value", dto.TotalStockCostValue.ToString("N2")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Stock Sell Value", dto.TotalStockSellValue.ToString("N2")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Stock Profit", dto.TotalStockProfit.ToString("N2")));

            model.Columns = new List<ReportPdfColumn>
            {
                new ReportPdfColumn("Name", 3.2f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Barcode", 2.0f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Category", 2.0f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Brand", 1.8f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Unit", 1.2f, ReportColumnAlignment.Center),
                new ReportPdfColumn("Price", 1.6f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Purchase Price", 1.8f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Profit Per Unit", 1.8f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Profit Margin", 1.6f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Stock Profit", 1.8f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Stock Qty", 1.5f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Alert Limit", 1.5f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Status", 1.6f, ReportColumnAlignment.Center)
            };

            string FormatStatus(string s) => s == "InStock" ? "In Stock" : (s == "LowStock" ? "Low Stock" : "Out of Stock");

            foreach (var item in dto.Items ?? Enumerable.Empty<StockReportRowDto>())
            {
                model.Rows.Add(new string[]
                {
                    item.Name ?? "—",
                    item.Barcode ?? "—",
                    item.CategoryName ?? "—",
                    item.BrandName ?? "—",
                    item.UnitName ?? "—",
                    item.Price.ToString("N2"),
                    item.CostPrice.ToString("N2"),
                    item.ProfitPerUnit.ToString("N2"),
                    item.ProfitMarginPercent.HasValue ? $"{item.ProfitMarginPercent.Value:N1}%" : "—",
                    item.StockProfit.ToString("N2"),
                    item.StockQuantity.ToString("N2"),
                    item.AlertQuantityLimit.ToString("N2"),
                    FormatStatus(item.Status)
                });
            }

            return model;
        }

        public static ReportPdfModel ToPdfModel(this SaleReportDto dto, ReportDateRangeInput input, string branchName = null)
        {
            var model = new ReportPdfModel
            {
                Title = "Sale Report",
                GeneratedAt = DateTime.Now,
                DateRange = FormatDateRange(input),
                BranchName = branchName,
                SearchKeyword = input?.Keyword
            };

            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Sales", (dto.Items?.Count ?? 0).ToString("N0")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Amount", dto.TotalAmount.ToString("N2")));

            model.Columns = new List<ReportPdfColumn>
            {
                new ReportPdfColumn("Invoice No", 2.2f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Sale Date", 2.0f, ReportColumnAlignment.Center),
                new ReportPdfColumn("Customer", 3.5f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Total Amount", 2.5f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Notes", 4.0f, ReportColumnAlignment.Left)
            };

            foreach (var item in dto.Items ?? Enumerable.Empty<SaleReportRowDto>())
            {
                model.Rows.Add(new string[]
                {
                    item.InvoiceNo ?? ("#" + item.Id),
                    item.SaleDate.ToString("yyyy-MM-dd"),
                    item.CustomerName ?? "—",
                    item.TotalAmount.ToString("N2"),
                    item.Notes ?? "—"
                });
            }

            model.FooterRows.Add(new string[]
            {
                "Total",
                "",
                "",
                dto.TotalAmount.ToString("N2"),
                ""
            });

            return model;
        }

        public static ReportPdfModel ToPdfModel(this PurchaseReportDto dto, ReportDateRangeInput input, string branchName = null)
        {
            var model = new ReportPdfModel
            {
                Title = "Purchase Report",
                GeneratedAt = DateTime.Now,
                DateRange = FormatDateRange(input),
                BranchName = branchName,
                SearchKeyword = input?.Keyword
            };

            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Purchases", (dto.Items?.Count ?? 0).ToString("N0")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Amount", dto.TotalAmount.ToString("N2")));

            model.Columns = new List<ReportPdfColumn>
            {
                new ReportPdfColumn("Invoice No", 2.2f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Purchase Date", 2.0f, ReportColumnAlignment.Center),
                new ReportPdfColumn("Supplier", 3.5f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Total Amount", 2.5f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Notes", 4.0f, ReportColumnAlignment.Left)
            };

            foreach (var item in dto.Items ?? Enumerable.Empty<PurchaseReportRowDto>())
            {
                model.Rows.Add(new string[]
                {
                    item.InvoiceNo ?? ("#" + item.Id),
                    item.PurchaseDate.ToString("yyyy-MM-dd"),
                    item.SupplierName ?? "—",
                    item.TotalAmount.ToString("N2"),
                    item.Notes ?? "—"
                });
            }

            model.FooterRows.Add(new string[]
            {
                "Total",
                "",
                "",
                dto.TotalAmount.ToString("N2"),
                ""
            });

            return model;
        }

        public static ReportPdfModel ToPdfModel(this ExpenseReportDto dto, ReportDateRangeInput input, string branchName = null)
        {
            var model = new ReportPdfModel
            {
                Title = "Expense Report",
                GeneratedAt = DateTime.Now,
                DateRange = FormatDateRange(input),
                BranchName = branchName,
                SearchKeyword = input?.Keyword
            };

            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Expenses", (dto.Items?.Count ?? 0).ToString("N0")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Amount", dto.TotalAmount.ToString("N2")));

            model.Columns = new List<ReportPdfColumn>
            {
                new ReportPdfColumn("Expense Date", 2.0f, ReportColumnAlignment.Center),
                new ReportPdfColumn("Reference No", 2.2f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Description", 4.0f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Payment Account", 3.0f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Created By", 2.2f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Amount", 2.2f, ReportColumnAlignment.Right)
            };

            foreach (var item in dto.Items ?? Enumerable.Empty<ExpenseReportRowDto>())
            {
                model.Rows.Add(new string[]
                {
                    item.ExpenseDate.ToString("yyyy-MM-dd"),
                    item.ReferenceNo ?? ("#" + item.Id),
                    item.Description ?? "—",
                    item.PaymentAccountName ?? "—",
                    item.CreatedByName ?? "—",
                    item.Amount.ToString("N2")
                });
            }

            model.FooterRows.Add(new string[]
            {
                "Total",
                "",
                "",
                "",
                "",
                dto.TotalAmount.ToString("N2")
            });

            return model;
        }

        public static ReportPdfModel ToPdfModel(this ProductProfitReportDto dto, ReportDateRangeInput input, string branchName = null)
        {
            var model = new ReportPdfModel
            {
                Title = "Product Profit Report",
                GeneratedAt = DateTime.Now,
                DateRange = FormatDateRange(input),
                BranchName = branchName,
                SearchKeyword = input?.Keyword
            };

            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Products Sold", dto.TotalProductsSold.ToString("N0")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Quantity Sold", dto.TotalQuantitySold.ToString("N2")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Revenue", dto.TotalRevenue.ToString("N2")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Profit", dto.TotalProfit.ToString("N2")));
            if (dto.AverageProfitMarginPercent.HasValue)
            {
                model.SummaryItems.Add(new KeyValuePair<string, string>("Avg Profit Margin", $"{dto.AverageProfitMarginPercent.Value:N1}%"));
            }

            model.Columns = new List<ReportPdfColumn>
            {
                new ReportPdfColumn("Product Name", 3.5f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Barcode", 2.0f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Category", 2.0f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Unit", 1.2f, ReportColumnAlignment.Center),
                new ReportPdfColumn("Qty Sold", 1.6f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Cost Price", 1.8f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Selling Price", 1.8f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Profit/Unit", 1.8f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Total Cost", 2.0f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Total Revenue", 2.0f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Total Profit", 2.0f, ReportColumnAlignment.Right),
                new ReportPdfColumn("Margin %", 1.6f, ReportColumnAlignment.Right)
            };

            foreach (var item in dto.Items ?? Enumerable.Empty<ProductProfitReportRowDto>())
            {
                model.Rows.Add(new string[]
                {
                    item.Name ?? "—",
                    item.Barcode ?? "—",
                    item.CategoryName ?? "—",
                    item.UnitName ?? "—",
                    item.QuantitySold.ToString("N2"),
                    item.CostPrice.ToString("N2"),
                    item.SellingPrice.ToString("N2"),
                    item.ProfitPerUnit.ToString("N2"),
                    item.TotalCost.ToString("N2"),
                    item.TotalRevenue.ToString("N2"),
                    item.TotalProfit.ToString("N2"),
                    item.ProfitMarginPercent.HasValue ? $"{item.ProfitMarginPercent.Value:N1}%" : "—"
                });
            }

            model.FooterRows.Add(new string[]
            {
                "Total",
                "",
                "",
                "",
                dto.TotalQuantitySold.ToString("N2"),
                "",
                "",
                "",
                dto.TotalCost.ToString("N2"),
                dto.TotalRevenue.ToString("N2"),
                dto.TotalProfit.ToString("N2"),
                dto.AverageProfitMarginPercent.HasValue ? $"{dto.AverageProfitMarginPercent.Value:N1}%" : ""
            });

            return model;
        }

        public static ReportPdfModel ToPdfModel(this BalanceSheetReportDto dto, ReportDateRangeInput input, string branchName = null)
        {
            var model = new ReportPdfModel
            {
                Title = "Balance Sheet & Financial Summary",
                GeneratedAt = DateTime.Now,
                DateRange = FormatDateRange(input),
                BranchName = branchName,
                SearchKeyword = input?.Keyword,
                StatusBanner = $"Accounting Equation: Assets ({dto.TotalAssets:N2}) = Liabilities ({dto.TotalLiabilities:N2}) + Equity ({dto.TotalEquity:N2}) — [{(dto.IsBalanced ? "Balanced" : "Balanced with Adjustments")}]"
            };

            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Assets", dto.TotalAssets.ToString("N2")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Liabilities", dto.TotalLiabilities.ToString("N2")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Total Equity", dto.TotalEquity.ToString("N2")));
            model.SummaryItems.Add(new KeyValuePair<string, string>("Net Profit/(Loss)", dto.NetProfitOrLoss.ToString("N2")));

            var colDef = new List<ReportPdfColumn>
            {
                new ReportPdfColumn("Category", 3.0f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Account Name", 4.0f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Code", 2.0f, ReportColumnAlignment.Center),
                new ReportPdfColumn("Type", 2.0f, ReportColumnAlignment.Left),
                new ReportPdfColumn("Balance / Amount", 2.5f, ReportColumnAlignment.Right)
            };

            // Section 1: Assets
            var assetSection = new ReportPdfSection
            {
                Title = "1. ASSETS",
                Columns = colDef,
                TotalLabel = "TOTAL ASSETS",
                TotalValue = dto.TotalAssets.ToString("N2")
            };
            foreach (var cat in dto.AssetCategories ?? Enumerable.Empty<BalanceSheetCategoryDto>())
            {
                foreach (var row in cat.Items ?? Enumerable.Empty<BalanceSheetRowDto>())
                {
                    assetSection.Rows.Add(new string[]
                    {
                        cat.CategoryName ?? "—",
                        row.AccountName ?? "—",
                        row.AccountCode ?? "—",
                        row.AccountType ?? "—",
                        row.Balance.ToString("N2")
                    });
                }
            }
            model.Sections.Add(assetSection);

            // Section 2: Liabilities
            var liabSection = new ReportPdfSection
            {
                Title = "2. LIABILITIES",
                Columns = colDef,
                TotalLabel = "TOTAL LIABILITIES",
                TotalValue = dto.TotalLiabilities.ToString("N2")
            };
            foreach (var cat in dto.LiabilityCategories ?? Enumerable.Empty<BalanceSheetCategoryDto>())
            {
                foreach (var row in cat.Items ?? Enumerable.Empty<BalanceSheetRowDto>())
                {
                    liabSection.Rows.Add(new string[]
                    {
                        cat.CategoryName ?? "—",
                        row.AccountName ?? "—",
                        row.AccountCode ?? "—",
                        row.AccountType ?? "—",
                        row.Balance.ToString("N2")
                    });
                }
            }
            model.Sections.Add(liabSection);

            // Section 3: Equity
            var eqSection = new ReportPdfSection
            {
                Title = "3. OWNER'S EQUITY & RETAINED EARNINGS",
                Columns = colDef,
                TotalLabel = "TOTAL EQUITY",
                TotalValue = dto.TotalEquity.ToString("N2")
            };
            foreach (var cat in dto.EquityCategories ?? Enumerable.Empty<BalanceSheetCategoryDto>())
            {
                foreach (var row in cat.Items ?? Enumerable.Empty<BalanceSheetRowDto>())
                {
                    eqSection.Rows.Add(new string[]
                    {
                        cat.CategoryName ?? "—",
                        row.AccountName ?? "—",
                        row.AccountCode ?? "—",
                        row.AccountType ?? "—",
                        row.Balance.ToString("N2")
                    });
                }
            }
            model.Sections.Add(eqSection);

            return model;
        }
    }
}
