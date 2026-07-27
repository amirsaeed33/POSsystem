using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Timing;
using Microsoft.EntityFrameworkCore;
using SmartPos.Dashboard.Dto;
using SmartPos.Expenses;
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

        public DashboardAppService(
            IRepository<Product> productRepository,
            IRepository<Sale> saleRepository,
            IRepository<Purchase> purchaseRepository,
            IRepository<Expense> expenseRepository)
        {
            _productRepository = productRepository;
            _saleRepository = saleRepository;
            _purchaseRepository = purchaseRepository;
            _expenseRepository = expenseRepository;
        }

        public async Task<DashboardDto> GetAsync()
        {
            var products = await _productRepository.GetAllIncluding(x => x.Category, x => x.Brand)
                .AsNoTracking()
                .ToListAsync();

            var inStock = products.Where(IsInStock).ToList();
            var lowStock = products.Where(IsLowStock).ToList();
            var outOfStock = products.Where(p => p.StockQuantity <= 0).ToList();

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
                .Select(x => new { x.SaleDate, x.TotalAmount })
                .ToListAsync();

            var purchases = await _purchaseRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.PurchaseDate >= startMonth)
                .Select(x => new { x.PurchaseDate, x.TotalAmount })
                .ToListAsync();

            var expenses = await _expenseRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.ExpenseDate >= startMonth)
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

            var todayProfit = todaySales - todayPurchases - todayExpenses;

            var costByProductId = products.ToDictionary(p => p.Id, p => p.CostPrice);

            var marginSales = await _saleRepository.GetAllIncluding(x => x.Lines)
                .AsNoTracking()
                .Where(x => x.SaleDate >= previousPeriodStart && x.SaleDate < currentPeriodEnd)
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
                .Select(p => new DashboardProductRowDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = "PR-" + p.Id.ToString("D3"),
                    CategoryName = p.Category?.Name,
                    BrandName = p.Brand?.Name,
                    Units = p.StockQuantity,
                    Price = p.Price,
                    CostPrice = p.CostPrice,
                    ProfitPerUnit = ProductPricing.ProfitPerUnit(p.Price, p.CostPrice),
                    Status = StatusOf(p),
                    ImagePath = p.ImagePath
                })
                .ToList();

            var user = await GetCurrentUserAsync();
            var userName = $"{user.Name} {user.Surname}".Trim();
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = user.UserName;
            }

            return new DashboardDto
            {
                UserDisplayName = userName,
                UserImageUrl = user.UserImageUrl,
                TotalProducts = products.Count,
                InStockCount = inStock.Count,
                LowStockCount = lowStock.Count,
                OutOfStockCount = outOfStock.Count,
                InStockUnits = inStock.Sum(p => p.StockQuantity),
                LowStockUnits = lowStock.Sum(p => p.StockQuantity),
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
                Products = productRows
            };
        }

        /// <summary>
        /// Previous equivalent period end (exclusive): same MTD day count in the previous month,
        /// clamped to the start of the current month.
        /// </summary>
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
                    var lineRevenue = line.LineTotal > 0
                        ? line.LineTotal
                        : line.UnitPrice * line.Quantity;
                    revenue += lineRevenue;

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

        private static (decimal GrowthPercentage, bool IsGrowthPositive) ComputeGrowthPercentage(
            decimal current,
            decimal previous)
        {
            decimal growth;
            if (previous == 0)
            {
                // Avoid divide-by-zero: no change when both zero; otherwise treat as +/-100%.
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

        private static bool IsOutOfStock(Product product) => product.StockQuantity <= 0;

        private static bool IsLowStock(Product product) =>
            product.StockQuantity > 0 && product.StockQuantity <= EffectiveAlertLimit(product);

        private static bool IsInStock(Product product) =>
            product.StockQuantity > EffectiveAlertLimit(product);

        private static string StatusOf(Product product)
        {
            if (IsOutOfStock(product)) return "OutOfStock";
            if (IsLowStock(product)) return "LowStock";
            return "InStock";
        }
    }
}
