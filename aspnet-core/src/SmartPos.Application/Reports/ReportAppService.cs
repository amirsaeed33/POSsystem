using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Dashboard;
using SmartPos.Expenses;
using SmartPos.Inventory;
using SmartPos.Products;
using SmartPos.Purchases;
using SmartPos.Reports.Dto;
using SmartPos.Sales;

namespace SmartPos.Reports
{
    [AbpAuthorize(PermissionNames.Pages_Reports)]
    public class ReportAppService : ApplicationService, IReportAppService
    {
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<Purchase> _purchaseRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IBranchStockManager _branchStockManager;

        public ReportAppService(
            IRepository<Sale> saleRepository,
            IRepository<Purchase> purchaseRepository,
            IRepository<Expense> expenseRepository,
            IRepository<Product> productRepository,
            IBranchStockManager branchStockManager)
        {
            _saleRepository = saleRepository;
            _purchaseRepository = purchaseRepository;
            _expenseRepository = expenseRepository;
            _productRepository = productRepository;
            _branchStockManager = branchStockManager;
        }

        public async Task<SaleReportDto> GetSaleReportAsync(ReportDateRangeInput input)
        {
            input ??= new ReportDateRangeInput();
            var (from, to) = NormalizeRange(input);

            var query = _saleRepository.GetAllIncluding(x => x.Customer)
                .AsNoTracking()
                .Where(x => x.SaleDate >= from && x.SaleDate < to)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.InvoiceNo != null && x.InvoiceNo.Contains(input.Keyword))
                         || (x.Customer != null && x.Customer.Name.Contains(input.Keyword))
                         || (x.Notes != null && x.Notes.Contains(input.Keyword)));

            var items = await query
                .OrderByDescending(x => x.SaleDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new SaleReportRowDto
                {
                    Id = x.Id,
                    InvoiceNo = x.InvoiceNo,
                    SaleDate = x.SaleDate,
                    CustomerName = x.Customer != null ? x.Customer.Name : null,
                    TotalAmount = x.TotalAmount,
                    Notes = x.Notes
                })
                .ToListAsync();

            return new SaleReportDto
            {
                Items = items,
                TotalAmount = items.Sum(x => x.TotalAmount)
            };
        }

        public async Task<PurchaseReportDto> GetPurchaseReportAsync(ReportDateRangeInput input)
        {
            input ??= new ReportDateRangeInput();
            var (from, to) = NormalizeRange(input);

            var query = _purchaseRepository.GetAllIncluding(x => x.Supplier)
                .AsNoTracking()
                .Where(x => x.PurchaseDate >= from && x.PurchaseDate < to)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.InvoiceNo != null && x.InvoiceNo.Contains(input.Keyword))
                         || (x.Supplier != null && x.Supplier.Name.Contains(input.Keyword))
                         || (x.Notes != null && x.Notes.Contains(input.Keyword)));

            var items = await query
                .OrderByDescending(x => x.PurchaseDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new PurchaseReportRowDto
                {
                    Id = x.Id,
                    InvoiceNo = x.InvoiceNo,
                    PurchaseDate = x.PurchaseDate,
                    SupplierName = x.Supplier != null ? x.Supplier.Name : null,
                    TotalAmount = x.TotalAmount,
                    Notes = x.Notes
                })
                .ToListAsync();

            return new PurchaseReportDto
            {
                Items = items,
                TotalAmount = items.Sum(x => x.TotalAmount)
            };
        }

        public async Task<ExpenseReportDto> GetExpenseReportAsync(ReportDateRangeInput input)
        {
            input ??= new ReportDateRangeInput();
            var (from, to) = NormalizeRange(input);

            var query = _expenseRepository.GetAllIncluding(x => x.PaymentAccount)
                .AsNoTracking()
                .Where(x => x.ExpenseDate >= from && x.ExpenseDate < to)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.ReferenceNo != null && x.ReferenceNo.Contains(input.Keyword))
                         || (x.Description != null && x.Description.Contains(input.Keyword))
                         || (x.PaymentAccount != null && x.PaymentAccount.Name.Contains(input.Keyword)));

            var items = await query
                .OrderByDescending(x => x.ExpenseDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new ExpenseReportRowDto
                {
                    Id = x.Id,
                    ExpenseDate = x.ExpenseDate,
                    ReferenceNo = x.ReferenceNo,
                    Description = x.Description,
                    PaymentAccountName = x.PaymentAccount != null ? x.PaymentAccount.Name : null,
                    Amount = x.Amount
                })
                .ToListAsync();

            return new ExpenseReportDto
            {
                Items = items,
                TotalAmount = items.Sum(x => x.Amount)
            };
        }

        public async Task<StockReportDto> GetStockReportAsync(ReportDateRangeInput input)
        {
            input ??= new ReportDateRangeInput();

            var products = await _productRepository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit)
                .AsNoTracking()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Barcode != null && x.Barcode.Contains(input.Keyword))
                         || (x.Category != null && x.Category.Name.Contains(input.Keyword))
                         || (x.Brand != null && x.Brand.Name.Contains(input.Keyword)))
                .OrderBy(x => x.Name)
                .ToListAsync();

            var stockByProductId = await _branchStockManager.GetAggregatedQuantitiesAsync(products.Select(p => p.Id));

            string StatusOf(Product p)
            {
                var stockQuantity = stockByProductId.TryGetValue(p.Id, out var qty) ? qty : 0;
                var limit = p.AlertQuantityLimit > 0
                    ? p.AlertQuantityLimit
                    : DashboardAppService.DefaultAlertQuantityLimit;
                if (stockQuantity <= 0) return "OutOfStock";
                if (stockQuantity <= limit) return "LowStock";
                return "InStock";
            }

            var items = products.Select(p =>
            {
                var stockQuantity = stockByProductId.TryGetValue(p.Id, out var qty) ? qty : 0;
                return new StockReportRowDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    CategoryName = p.Category?.Name,
                    BrandName = p.Brand?.Name,
                    UnitName = p.Unit?.Name,
                    Price = p.Price,
                    CostPrice = p.CostPrice,
                    ProfitPerUnit = ProductPricing.ProfitPerUnit(p.Price, p.CostPrice),
                    ProfitMarginPercent = ProductPricing.ProfitMarginPercent(p.Price, p.CostPrice),
                    StockProfit = ProductPricing.StockProfit(p.Price, p.CostPrice, stockQuantity),
                    StockQuantity = stockQuantity,
                    AlertQuantityLimit = p.AlertQuantityLimit,
                    Status = StatusOf(p)
                };
            }).ToList();

            return new StockReportDto
            {
                Items = items,
                TotalProducts = items.Count,
                InStockCount = items.Count(x => x.Status == "InStock"),
                LowStockCount = items.Count(x => x.Status == "LowStock"),
                OutOfStockCount = items.Count(x => x.Status == "OutOfStock"),
                TotalStockUnits = items.Sum(x => x.StockQuantity),
                TotalStockCostValue = items.Sum(x => x.CostPrice * x.StockQuantity),
                TotalStockSellValue = items.Sum(x => x.Price * x.StockQuantity),
                TotalStockProfit = items.Sum(x => x.StockProfit)
            };
        }

        private static (DateTime from, DateTime toExclusive) NormalizeRange(ReportDateRangeInput input)
        {
            var now = Abp.Timing.Clock.Now;
            var from = input.FromDate?.Date ?? new DateTime(now.Year, now.Month, 1);
            var toDate = input.ToDate?.Date ?? now.Date;
            var toExclusive = toDate.AddDays(1);
            if (toExclusive <= from)
            {
                toExclusive = from.AddDays(1);
            }

            return (from, toExclusive);
        }
    }
}
