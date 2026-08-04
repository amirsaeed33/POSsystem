using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using Microsoft.EntityFrameworkCore;
using SmartPos.Branches;
using SmartPos.CompanyProfiles;
using SmartPos.Dashboard.Dto;
using SmartPos.Expenses;
using SmartPos.Inventory;
using SmartPos.Orders;
using SmartPos.Products;
using SmartPos.Purchases;
using SmartPos.Sales;

namespace SmartPos.Dashboard
{
    [AbpAuthorize]
    public class DashboardAppService : SmartPosAppServiceBase, IDashboardAppService
    {
        public const decimal DefaultAlertQuantityLimit = 10;

        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<Purchase> _purchaseRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<CustomerOrder> _customerOrderRepository;
        private readonly IRepository<StockAdjustment> _stockAdjustmentRepository;
        private readonly IRepository<CompanyProfile> _companyProfileRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchStockManager _branchStockManager;

        public DashboardAppService(
            IRepository<Product> productRepository,
            IRepository<Sale> saleRepository,
            IRepository<Purchase> purchaseRepository,
            IRepository<Expense> expenseRepository,
            IRepository<CustomerOrder> customerOrderRepository,
            IRepository<StockAdjustment> stockAdjustmentRepository,
            IRepository<CompanyProfile> companyProfileRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchStockManager branchStockManager)
        {
            _productRepository = productRepository;
            _saleRepository = saleRepository;
            _purchaseRepository = purchaseRepository;
            _expenseRepository = expenseRepository;
            _customerOrderRepository = customerOrderRepository;
            _stockAdjustmentRepository = stockAdjustmentRepository;
            _companyProfileRepository = companyProfileRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchStockManager = branchStockManager;
        }

        public async Task<DashboardDto> GetAsync()
        {
            var branchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();

            var products = await _productRepository.GetAllIncluding(x => x.Category, x => x.Brand)
                .AsNoTracking()
                .ToListAsync();

            var stockByProductId = await ResolveBranchStockAsync(branchId, products.Select(p => p.Id));

            var inStock = products.Where(p => IsInStock(p, stockByProductId)).ToList();
            var lowStock = products.Where(p => IsLowStock(p, stockByProductId)).ToList();
            var outOfStock = products.Where(p => GetStock(p, stockByProductId) <= 0).ToList();

            var now = Clock.Now;
            var todayStart = now.Date;
            var tomorrow = todayStart.AddDays(1);
            var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

            var currentPeriodStart = new DateTime(now.Year, now.Month, 1);
            var currentPeriodEnd = tomorrow;
            var previousPeriodStart = currentPeriodStart.AddMonths(-1);
            var previousPeriodEnd = EquivalentPreviousPeriodEnd(now, currentPeriodStart, previousPeriodStart);

            var sales = await _saleRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.SaleDate >= startMonth)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .Select(x => new { x.SaleDate, x.TotalAmount })
                .ToListAsync();

            var purchases = await _purchaseRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.PurchaseDate >= startMonth)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .Select(x => new { x.PurchaseDate, x.TotalAmount })
                .ToListAsync();

            var expenses = await _expenseRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.ExpenseDate >= startMonth)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .Select(x => new { x.ExpenseDate, x.Amount })
                .ToListAsync();

            var todaySales = sales
                .Where(x => x.SaleDate >= todayStart && x.SaleDate < tomorrow)
                .Sum(x => x.TotalAmount);

            var todayPurchases = purchases
                .Where(x => x.PurchaseDate >= todayStart && x.PurchaseDate < tomorrow)
                .Sum(x => x.TotalAmount);

            var todayExpenses = expenses
                .Where(x => x.ExpenseDate >= todayStart && x.ExpenseDate < tomorrow)
                .Sum(x => x.Amount);

            var costByProductId = products.ToDictionary(p => p.Id, p => p.CostPrice);

            var todaySaleLines = await _saleRepository.GetAllIncluding(x => x.Lines)
                .AsNoTracking()
                .Where(x => x.SaleDate >= todayStart && x.SaleDate < tomorrow)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .ToListAsync();
            var todayCogs = CalculateCostOfGoodsSold(todaySaleLines, costByProductId);
            var todayProfit = Math.Round(todaySales - todayCogs - todayExpenses, 2);

            var marginSales = await _saleRepository.GetAllIncluding(x => x.Lines)
                .AsNoTracking()
                .Where(x => x.SaleDate >= previousPeriodStart && x.SaleDate < currentPeriodEnd)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .ToListAsync();

            var currentMarginSales = marginSales
                .Where(x => x.SaleDate >= currentPeriodStart && x.SaleDate < currentPeriodEnd);
            var previousMarginSales = marginSales
                .Where(x => x.SaleDate >= previousPeriodStart && x.SaleDate < previousPeriodEnd);

            var averageProfitMargin = CalculateAverageProfitMargin(currentMarginSales, costByProductId);
            var previousAverageProfitMargin = CalculateAverageProfitMargin(previousMarginSales, costByProductId);
            var (growthPercentage, isGrowthPositive) = ComputeGrowthPercentage(
                averageProfitMargin,
                previousAverageProfitMargin);

            var cashFlow = new List<MonthlyCashFlowDto>();
            for (var i = 0; i < 12; i++)
            {
                var monthDate = startMonth.AddMonths(i);
                var income = sales
                    .Where(x => x.SaleDate.Year == monthDate.Year && x.SaleDate.Month == monthDate.Month)
                    .Sum(x => x.TotalAmount);
                var expense = expenses
                    .Where(x => x.ExpenseDate.Year == monthDate.Year && x.ExpenseDate.Month == monthDate.Month)
                    .Sum(x => x.Amount);
                var monthPurchases = purchases
                    .Where(x => x.PurchaseDate.Year == monthDate.Year && x.PurchaseDate.Month == monthDate.Month)
                    .Sum(x => x.TotalAmount);

                cashFlow.Add(new MonthlyCashFlowDto
                {
                    Year = monthDate.Year,
                    Month = monthDate.Month,
                    MonthLabel = monthDate.ToString("MMM", CultureInfo.InvariantCulture),
                    Income = income,
                    Expense = expense,
                    Purchases = monthPurchases
                });
            }

            var productRows = products
                .OrderByDescending(p => p.Id)
                .Take(8)
                .Select(p =>
                {
                    var units = GetStock(p, stockByProductId);
                    return new DashboardProductRowDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Sku = "PR-" + p.Id.ToString("D3"),
                        CategoryName = p.Category?.Name,
                        BrandName = p.Brand?.Name,
                        Units = units,
                        Price = p.Price,
                        CostPrice = p.CostPrice,
                        ProfitPerUnit = ProductPricing.ProfitPerUnit(p.Price, p.CostPrice),
                        Status = StatusOf(p, stockByProductId),
                        ImagePath = p.ImagePath
                    };
                })
                .ToList();

            var user = await GetCurrentUserAsync();
            var userName = $"{user.Name} {user.Surname}".Trim();
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = user.UserName;
            }

            var widgetData = await LoadWidgetDataAsync(todayStart, tomorrow, lowStock.Count, branchId);

            return new DashboardDto
            {
                UserDisplayName = userName,
                UserImageUrl = user.UserImageUrl,
                TotalProducts = products.Count,
                InStockCount = inStock.Count,
                LowStockCount = lowStock.Count,
                OutOfStockCount = outOfStock.Count,
                InStockUnits = inStock.Sum(p => GetStock(p, stockByProductId)),
                LowStockUnits = lowStock.Sum(p => GetStock(p, stockByProductId)),
                LowStockThreshold = (int)DefaultAlertQuantityLimit,
                TodaySales = todaySales,
                TodayPurchases = todayPurchases,
                TodayExpenses = todayExpenses,
                TodayProfit = todayProfit,
                AverageProfitMargin = averageProfitMargin,
                PreviousAverageProfitMargin = previousAverageProfitMargin,
                GrowthPercentage = growthPercentage,
                IsGrowthPositive = isGrowthPositive,
                CashFlow = cashFlow,
                Products = productRows,
                QuickActions = widgetData.QuickActions,
                LatestListTitle = widgetData.LatestListTitle,
                LatestListItems = widgetData.LatestListItems,
                Timeline = widgetData.Timeline
            };
        }

        private async Task<Dictionary<int, decimal>> ResolveBranchStockAsync(
            int? branchId,
            IEnumerable<int> productIds)
        {
            if (!branchId.HasValue)
            {
                return new Dictionary<int, decimal>();
            }

            return await _branchStockManager.GetQuantitiesAsync(branchId.Value, productIds);
        }

        private async Task<(
            DashboardQuickActionCountsDto QuickActions,
            string LatestListTitle,
            List<DashboardLatestListItemDto> LatestListItems,
            List<DashboardTimelineEventDto> Timeline)> LoadWidgetDataAsync(
            DateTime todayStart,
            DateTime tomorrow,
            int lowStockCount,
            int? branchId)
        {
            var pendingOrdersQuery = _customerOrderRepository.GetAll()
                .Where(x => x.Status == CustomerOrderStatus.Pending)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value);
            var pendingOrdersCount = await pendingOrdersQuery.CountAsync();

            var companyProfileCount = await _companyProfileRepository.CountAsync();

            var todaySalesCount = await _saleRepository.GetAll()
                .Where(x => x.SaleDate >= todayStart && x.SaleDate < tomorrow)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .CountAsync();

            var recentSales = await _saleRepository.GetAllIncluding(x => x.Customer)
                .AsNoTracking()
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .OrderByDescending(x => x.SaleDate)
                .Take(50)
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNo,
                    x.SaleDate,
                    x.TotalAmount,
                    x.CustomerId,
                    CustomerName = x.Customer != null ? x.Customer.Name : null
                })
                .ToListAsync();

            var recentPurchases = await _purchaseRepository.GetAll()
                .AsNoTracking()
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .OrderByDescending(x => x.PurchaseDate)
                .Take(10)
                .Select(x => new { x.Id, x.InvoiceNo, x.PurchaseDate, x.TotalAmount })
                .ToListAsync();

            var recentExpenses = await _expenseRepository.GetAll()
                .AsNoTracking()
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .OrderByDescending(x => x.ExpenseDate)
                .Take(10)
                .Select(x => new { x.Id, x.ReferenceNo, x.ExpenseDate, x.Amount })
                .ToListAsync();

            var recentAdjustments = await _stockAdjustmentRepository.GetAllIncluding(x => x.Lines)
                .AsNoTracking()
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .OrderByDescending(x => x.AdjustmentDate)
                .Take(10)
                .ToListAsync();

            var (latestTitle, latestItems) = BuildLatestListFromSales(
                recentSales.Select(sale => (
                    sale.Id,
                    sale.InvoiceNo,
                    sale.SaleDate,
                    sale.TotalAmount,
                    (int?)sale.CustomerId,
                    sale.CustomerName
                )));

            var timeline = new List<DashboardTimelineEventDto>();
            timeline.AddRange(recentSales.Take(10).Select(sale => new DashboardTimelineEventDto
            {
                Type = "sale",
                Title = "Sale #" + (string.IsNullOrWhiteSpace(sale.InvoiceNo) ? sale.Id.ToString() : sale.InvoiceNo),
                Amount = sale.TotalAmount,
                OccurredAt = sale.SaleDate
            }));
            timeline.AddRange(recentPurchases.Select(purchase => new DashboardTimelineEventDto
            {
                Type = "purchase",
                Title = "Purchase #" + (string.IsNullOrWhiteSpace(purchase.InvoiceNo) ? purchase.Id.ToString() : purchase.InvoiceNo),
                Amount = purchase.TotalAmount,
                OccurredAt = purchase.PurchaseDate
            }));
            timeline.AddRange(recentExpenses.Select(expense => new DashboardTimelineEventDto
            {
                Type = "expense",
                Title = "Expense #" + (string.IsNullOrWhiteSpace(expense.ReferenceNo) ? expense.Id.ToString() : expense.ReferenceNo),
                Amount = expense.Amount,
                OccurredAt = expense.ExpenseDate
            }));
            timeline.AddRange(recentAdjustments.Select(adj =>
            {
                var qty = (adj.Lines ?? Enumerable.Empty<StockAdjustmentLine>())
                    .Sum(line => line.QuantityChange);
                var qtyLabel = qty == 0
                    ? "Stock adjusted"
                    : (qty > 0 ? "+" : "") + qty.ToString(CultureInfo.InvariantCulture) + " units";
                return new DashboardTimelineEventDto
                {
                    Type = "stock",
                    Title = "Stock Adjusted #" + (string.IsNullOrWhiteSpace(adj.ReferenceNo) ? adj.Id.ToString() : adj.ReferenceNo),
                    Amount = 0,
                    QuantityLabel = qtyLabel,
                    OccurredAt = adj.AdjustmentDate
                };
            }));

            timeline = timeline
                .OrderByDescending(x => x.OccurredAt)
                .Take(40)
                .ToList();

            return (
                new DashboardQuickActionCountsDto
                {
                    LowStockCount = lowStockCount,
                    PendingOrdersCount = pendingOrdersCount,
                    TodaySalesCount = todaySalesCount,
                    CompanyProfileCount = companyProfileCount
                },
                latestTitle,
                latestItems,
                timeline);
        }

        private static (string Title, List<DashboardLatestListItemDto> Items) BuildLatestListFromSales(
            IEnumerable<(int Id, string InvoiceNo, DateTime SaleDate, decimal TotalAmount, int? CustomerId, string CustomerName)> recentSales)
        {
            var sales = recentSales.ToList();
            var customers = new List<DashboardLatestListItemDto>();
            var seen = new HashSet<int>();

            foreach (var sale in sales)
            {
                if (!sale.CustomerId.HasValue ||
                    string.IsNullOrWhiteSpace(sale.CustomerName) ||
                    seen.Contains(sale.CustomerId.Value))
                {
                    continue;
                }

                seen.Add(sale.CustomerId.Value);
                customers.Add(new DashboardLatestListItemDto
                {
                    Title = sale.CustomerName,
                    Subtitle = FormatMoney(sale.TotalAmount) + " · " + FormatDate(sale.SaleDate),
                    Initials = GetInitials(sale.CustomerName)
                });

                if (customers.Count >= 6)
                {
                    break;
                }
            }

            if (customers.Count > 0)
            {
                return ("Latest Customers", customers);
            }

            var latestSales = sales.Take(6).Select(sale =>
            {
                var title = !string.IsNullOrWhiteSpace(sale.CustomerName)
                    ? sale.CustomerName
                    : (!string.IsNullOrWhiteSpace(sale.InvoiceNo) ? sale.InvoiceNo : "Sale #" + sale.Id);
                return new DashboardLatestListItemDto
                {
                    Title = title,
                    Subtitle = FormatMoney(sale.TotalAmount) + " · " + FormatDate(sale.SaleDate),
                    Initials = GetInitials(title)
                };
            }).ToList();

            return ("Latest Sales", latestSales);
        }

        private static string FormatMoney(decimal amount)
        {
            return "PKR " + amount.ToString("N0", CultureInfo.GetCultureInfo("en-PK"));
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
        }

        private static string GetInitials(string name)
        {
            var parts = (name ?? string.Empty)
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "?";
            }

            if (parts.Length == 1)
            {
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            }

            return (parts[0][0].ToString() + parts[1][0]).ToUpperInvariant();
        }

        private static DateTime EquivalentPreviousPeriodEnd(
            DateTime now,
            DateTime currentPeriodStart,
            DateTime previousPeriodStart)
        {
            var dayCount = (now.Date - currentPeriodStart).Days + 1;
            var previousEnd = previousPeriodStart.AddDays(dayCount);
            return previousEnd > currentPeriodStart ? currentPeriodStart : previousEnd;
        }

        private static decimal CalculateAverageProfitMargin(
            IEnumerable<Sale> sales,
            IReadOnlyDictionary<int, decimal> costByProductId)
        {
            decimal revenue = 0;
            decimal cost = 0;

            foreach (var sale in sales)
            {
                if (sale.Lines == null)
                {
                    continue;
                }

                foreach (var line in sale.Lines)
                {
                    revenue += LineRevenue(line);
                    if (costByProductId.TryGetValue(line.ProductId, out var unitCost))
                    {
                        cost += unitCost * line.Quantity;
                    }
                }
            }

            if (revenue <= 0)
            {
                return 0;
            }

            return Math.Round((revenue - cost) / revenue * 100m, 2);
        }

        private static decimal CalculateCostOfGoodsSold(
            IEnumerable<Sale> sales,
            IReadOnlyDictionary<int, decimal> costByProductId)
        {
            decimal cost = 0;
            foreach (var sale in sales)
            {
                if (sale.Lines == null)
                {
                    continue;
                }

                foreach (var line in sale.Lines)
                {
                    if (costByProductId.TryGetValue(line.ProductId, out var unitCost))
                    {
                        cost += unitCost * line.Quantity;
                    }
                }
            }

            return cost;
        }

        private static decimal LineRevenue(SaleLine line)
        {
            return line.LineTotal > 0
                ? line.LineTotal
                : line.UnitPrice * line.Quantity;
        }

        private static (decimal GrowthPercentage, bool IsGrowthPositive) ComputeGrowthPercentage(
            decimal current,
            decimal previous)
        {
            decimal growth;
            if (previous == 0)
            {
                growth = current == 0 ? 0 : (current > 0 ? 100m : -100m);
            }
            else
            {
                growth = Math.Round((current - previous) / previous * 100m, 2);
            }

            return (growth, growth >= 0);
        }

        private static decimal EffectiveAlertLimit(Product product)
        {
            return product.AlertQuantityLimit > 0
                ? product.AlertQuantityLimit
                : DefaultAlertQuantityLimit;
        }

        private static decimal GetStock(Product product, IReadOnlyDictionary<int, decimal> stockByProductId)
        {
            if (stockByProductId != null && stockByProductId.Count > 0)
            {
                return stockByProductId.TryGetValue(product.Id, out var qty) ? qty : 0;
            }

            return product.StockQuantity;
        }

        private static bool IsOutOfStock(Product product, IReadOnlyDictionary<int, decimal> stockByProductId) =>
            GetStock(product, stockByProductId) <= 0;

        private static bool IsLowStock(Product product, IReadOnlyDictionary<int, decimal> stockByProductId)
        {
            var qty = GetStock(product, stockByProductId);
            return qty > 0 && qty <= EffectiveAlertLimit(product);
        }

        private static bool IsInStock(Product product, IReadOnlyDictionary<int, decimal> stockByProductId) =>
            GetStock(product, stockByProductId) > EffectiveAlertLimit(product);

        private static string StatusOf(Product product, IReadOnlyDictionary<int, decimal> stockByProductId)
        {
            if (IsOutOfStock(product, stockByProductId)) return "OutOfStock";
            if (IsLowStock(product, stockByProductId)) return "LowStock";
            return "InStock";
        }
    }
}
