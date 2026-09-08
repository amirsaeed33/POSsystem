using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Dashboard;
using SmartPos.Expenses;
using SmartPos.Inventory;
using SmartPos.Products;
using SmartPos.Purchases;
using SmartPos.Reports.Dto;
using SmartPos.Accounts;
using SmartPos.Customers;
using SmartPos.Sales;
using SmartPos.Suppliers;

namespace SmartPos.Reports
{
    [AbpAuthorize(PermissionNames.Pages_Reports)]
    public class ReportAppService : ApplicationService, IReportAppService
    {
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<Purchase> _purchaseRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IRepository<BranchStock> _branchStockRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly IBranchStockManager _branchStockManager;
        private readonly IRepository<BusinessAccount> _accountRepository;
        private readonly AccountBalanceManager _accountBalanceManager;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Supplier> _supplierRepository;

        public ReportAppService(
            IRepository<Sale> saleRepository,
            IRepository<Purchase> purchaseRepository,
            IRepository<Expense> expenseRepository,
            IRepository<Product> productRepository,
            IRepository<User, long> userRepository,
            IRepository<BranchStock> branchStockRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            IBranchStockManager branchStockManager,
            IRepository<BusinessAccount> accountRepository,
            AccountBalanceManager accountBalanceManager,
            IRepository<Customer> customerRepository,
            IRepository<Supplier> supplierRepository)
        {
            _saleRepository = saleRepository;
            _purchaseRepository = purchaseRepository;
            _expenseRepository = expenseRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _branchStockRepository = branchStockRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _branchStockManager = branchStockManager;
            _accountRepository = accountRepository;
            _accountBalanceManager = accountBalanceManager;
            _customerRepository = customerRepository;
            _supplierRepository = supplierRepository;
        }

        public async Task<SaleReportDto> GetSaleReportAsync(ReportDateRangeInput input)
        {
            input ??= new ReportDateRangeInput();
            var (from, to) = NormalizeRange(input);
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            var query = _saleRepository.GetAllIncluding(x => x.Customer)
                .AsNoTracking()
                .Where(x => x.SaleDate >= from && x.SaleDate < to)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
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
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            var query = _purchaseRepository.GetAllIncluding(x => x.Supplier)
                .AsNoTracking()
                .Where(x => x.PurchaseDate >= from && x.PurchaseDate < to)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
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
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            var query = _expenseRepository.GetAllIncluding(x => x.PaymentAccount)
                .AsNoTracking()
                .Where(x => x.ExpenseDate >= from && x.ExpenseDate < to)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.ReferenceNo != null && x.ReferenceNo.Contains(input.Keyword))
                         || (x.Description != null && x.Description.Contains(input.Keyword))
                         || (x.PaymentAccount != null && x.PaymentAccount.Name.Contains(input.Keyword)));

            var rawItems = await query
                .OrderByDescending(x => x.ExpenseDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.ExpenseDate,
                    x.ReferenceNo,
                    x.Description,
                    PaymentAccountName = x.PaymentAccount != null ? x.PaymentAccount.Name : null,
                    x.Amount,
                    x.CreatorUserId
                })
                .ToListAsync();

            var creatorIds = rawItems.Where(x => x.CreatorUserId.HasValue).Select(x => x.CreatorUserId.Value).Distinct().ToList();
            var creatorMap = await _userRepository.GetAll()
                .AsNoTracking()
                .Where(u => creatorIds.Contains(u.Id))
                .Select(u => new { u.Id, FullName = (u.Name + " " + u.Surname).Trim() })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            var items = rawItems.Select(x => new ExpenseReportRowDto
            {
                Id = x.Id,
                ExpenseDate = x.ExpenseDate,
                ReferenceNo = x.ReferenceNo,
                Description = x.Description,
                PaymentAccountName = x.PaymentAccountName,
                Amount = x.Amount,
                CreatedByName = x.CreatorUserId.HasValue && creatorMap.TryGetValue(x.CreatorUserId.Value, out var name) ? name : null
            }).ToList();

            return new ExpenseReportDto
            {
                Items = items,
                TotalAmount = items.Sum(x => x.Amount)
            };
        }

        public async Task<StockReportDto> GetStockReportAsync(ReportDateRangeInput input)
        {
            input ??= new ReportDateRangeInput();

            var branchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();

            var productQuery = _productRepository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit)
                .AsNoTracking()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Barcode != null && x.Barcode.Contains(input.Keyword))
                         || (x.Category != null && x.Category.Name.Contains(input.Keyword))
                         || (x.Brand != null && x.Brand.Name.Contains(input.Keyword)));

            if (branchId.HasValue)
            {
                productQuery = productQuery.WhereVisibleToBranch(
                    _branchStockRepository.GetAll(),
                    branchId.Value);
            }

            var products = await productQuery
                .OrderBy(x => x.Name)
                .ToListAsync();

            var effectiveBranchId = branchId ?? (await _branchAccessChecker.RequireEffectiveBranchIdAsync());
            var branchInfo = await _branchStockManager.GetBranchProductInfoAsync(effectiveBranchId, products.Select(p => p.Id));

            string StatusOf(Product p, decimal qty)
            {
                var limit = p.AlertQuantityLimit > 0
                    ? p.AlertQuantityLimit
                    : DashboardAppService.DefaultAlertQuantityLimit;
                if (qty <= 0) return "OutOfStock";
                if (qty <= limit) return "LowStock";
                return "InStock";
            }

            var items = products.Select(p =>
            {
                var info = branchInfo.TryGetValue(p.Id, out var row)
                    ? row
                    : new BranchProductInfo
                    {
                        ProductId = p.Id,
                        Quantity = 0,
                        Price = p.Price,
                        WholesalePrice = p.WholesalePrice,
                        CostPrice = p.CostPrice
                    };
                var price = info.Price;
                var cost = info.CostPrice;
                var qty = info.Quantity;
                return new StockReportRowDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    CategoryName = p.Category?.Name,
                    BrandName = p.Brand?.Name,
                    UnitName = p.Unit?.Name,
                    Price = price,
                    CostPrice = cost,
                    ProfitPerUnit = ProductPricing.ProfitPerUnit(price, cost),
                    ProfitMarginPercent = ProductPricing.ProfitMarginPercent(price, cost),
                    StockProfit = ProductPricing.StockProfit(price, cost, qty),
                    StockQuantity = qty,
                    AlertQuantityLimit = p.AlertQuantityLimit,
                    Status = StatusOf(p, qty)
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

        public async Task<ProductProfitReportDto> GetProductProfitReportAsync(ReportDateRangeInput input)
        {
            input ??= new ReportDateRangeInput();
            var (from, to) = NormalizeRange(input);
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            // Get all sales within date range for this branch
            var salesQuery = _saleRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.SaleDate >= from && x.SaleDate < to)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value);

            // Get sale IDs to filter sale lines
            var saleIds = await salesQuery.Select(x => x.Id).ToListAsync();

            if (saleIds.Count == 0)
            {
                return new ProductProfitReportDto
                {
                    Items = new List<ProductProfitReportRowDto>(),
                    TotalProductsSold = 0,
                    TotalQuantitySold = 0,
                    TotalCost = 0,
                    TotalRevenue = 0,
                    TotalProfit = 0,
                    AverageProfitMarginPercent = 0
                };
            }

            // Get all sale lines for those sales, grouped and aggregated by product
            var saleLines = await _saleRepository.GetAll()
                .Where(x => saleIds.Contains(x.Id))
                .SelectMany(x => x.Lines)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.LineTotal),
                    AverageUnitPrice = g.Average(x => x.UnitPrice)
                })
                .ToListAsync();

            if (saleLines.Count == 0)
            {
                return new ProductProfitReportDto
                {
                    Items = new List<ProductProfitReportRowDto>(),
                    TotalProductsSold = 0,
                    TotalQuantitySold = 0,
                    TotalCost = 0,
                    TotalRevenue = 0,
                    TotalProfit = 0,
                    AverageProfitMarginPercent = 0
                };
            }

            // Get products for those sale lines
            var productIds = saleLines.Select(x => x.ProductId).ToList();
            var products = await _productRepository.GetAllIncluding(x => x.Category, x => x.Unit)
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword) || (x.Barcode != null && x.Barcode.Contains(input.Keyword)))
                .ToListAsync();

            // Build the report
            var items = new List<ProductProfitReportRowDto>();

            foreach (var product in products)
            {
                var saleData = saleLines.FirstOrDefault(x => x.ProductId == product.Id);
                if (saleData == null) continue;

                var quantitySold = saleData.QuantitySold;
                var totalRevenue = saleData.TotalRevenue;
                var unitPrice = saleData.AverageUnitPrice;
                var costPrice = product.CostPrice;

                var totalCost = quantitySold * costPrice;
                var totalProfit = totalRevenue - totalCost;
                var profitPerUnit = unitPrice - costPrice;
                var profitMarginPercent = unitPrice > 0 ? (profitPerUnit / unitPrice) * 100 : 0;

                items.Add(new ProductProfitReportRowDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Barcode = product.Barcode,
                    CategoryName = product.Category?.Name,
                    UnitName = product.Unit?.Name,
                    QuantitySold = quantitySold,
                    CostPrice = costPrice,
                    SellingPrice = unitPrice,
                    ProfitPerUnit = profitPerUnit,
                    TotalCost = totalCost,
                    TotalRevenue = totalRevenue,
                    TotalProfit = totalProfit,
                    ProfitMarginPercent = profitMarginPercent
                });
            }

            items = items
                .OrderByDescending(x => x.TotalProfit)
                .ThenByDescending(x => x.QuantitySold)
                .ToList();

            var totalQuantitySold = items.Sum(x => x.QuantitySold);
            var totalCostAll = items.Sum(x => x.TotalCost);
            var totalRevenueAll = items.Sum(x => x.TotalRevenue);
            var totalProfitAll = items.Sum(x => x.TotalProfit);
            var averageMarginPercent = totalRevenueAll > 0 
                ? ((totalRevenueAll - totalCostAll) / totalRevenueAll) * 100 
                : 0;

            return new ProductProfitReportDto
            {
                Items = items,
                TotalProductsSold = items.Count,
                TotalQuantitySold = totalQuantitySold,
                TotalCost = totalCostAll,
                TotalRevenue = totalRevenueAll,
                TotalProfit = totalProfitAll,
                AverageProfitMarginPercent = averageMarginPercent
            };
        }

        public async Task<BalanceSheetReportDto> GetBalanceSheetReportAsync(ReportDateRangeInput input)
        {
            var stockReport = await GetStockReportAsync(input);
            var totalStockCostValue = stockReport?.TotalStockCostValue ?? 0;

            // Get business accounts and balances
            var allAccounts = await _accountRepository.GetAllIncluding(a => a.AccountTypeLookup)
                .AsNoTracking()
                .Where(a => a.IsActive)
                .ToListAsync();

            var cashAndBankItems = new List<BalanceSheetRowDto>();
            var otherAssetItems = new List<BalanceSheetRowDto>();
            var liabilityItems = new List<BalanceSheetRowDto>();
            var equityItems = new List<BalanceSheetRowDto>();

            decimal liquidCashBankTotal = 0;
            decimal totalInitialCapital = 0;

            foreach (var account in allAccounts)
            {
                await _accountBalanceManager.EnsureOpeningBalancePostedAsync(account);
                var balance = await _accountBalanceManager.GetBalanceAsync(account.Id);
                var accountType = account.AccountTypeLookup?.DisplayName ?? account.AccountTypeLookup?.Name ?? account.AccountType ?? "";
                totalInitialCapital += account.OpeningBalance;

                var row = new BalanceSheetRowDto
                {
                    AccountName = account.Name,
                    AccountCode = account.Code,
                    AccountType = accountType,
                    Balance = balance
                };

                if (accountType.Equals("Cash", StringComparison.OrdinalIgnoreCase) ||
                    accountType.Equals("Bank", StringComparison.OrdinalIgnoreCase) ||
                    accountType.Equals("Mobile Wallet", StringComparison.OrdinalIgnoreCase) ||
                    account.Code == Accounts.SystemAccountCodes.Cash ||
                    account.Code == Accounts.SystemAccountCodes.Bank)
                {
                    cashAndBankItems.Add(row);
                    liquidCashBankTotal += balance;
                }
                else if (accountType.Equals("Asset", StringComparison.OrdinalIgnoreCase) ||
                         accountType.Equals("Current Asset", StringComparison.OrdinalIgnoreCase))
                {
                    otherAssetItems.Add(row);
                }
                else if (accountType.Equals("Liability", StringComparison.OrdinalIgnoreCase) ||
                         accountType.Equals("Current Liability", StringComparison.OrdinalIgnoreCase))
                {
                    liabilityItems.Add(row);
                }
                else if (accountType.Equals("Equity", StringComparison.OrdinalIgnoreCase) ||
                         accountType.Equals("Capital", StringComparison.OrdinalIgnoreCase))
                {
                    equityItems.Add(row);
                }
            }

            // Customer Receivables (Accounts Receivable)
            var customers = await _customerRepository.GetAll().AsNoTracking().Where(c => c.AccountId.HasValue).ToListAsync();
            decimal totalCustomerReceivables = 0;
            foreach (var customer in customers)
            {
                var bal = await _accountBalanceManager.GetBalanceAsync(customer.AccountId.Value);
                if (bal > 0)
                {
                    totalCustomerReceivables += bal;
                }
            }

            if (totalCustomerReceivables != 0)
            {
                otherAssetItems.Add(new BalanceSheetRowDto
                {
                    AccountName = "Accounts Receivable (Customer Receivables)",
                    AccountCode = "AR-CUST",
                    AccountType = "Asset",
                    Balance = totalCustomerReceivables
                });
            }

            // Inventory Stock Valuation Asset
            if (totalStockCostValue > 0)
            {
                otherAssetItems.Add(new BalanceSheetRowDto
                {
                    AccountName = "Inventory Stock (Cost Valuation)",
                    AccountCode = "INV-STOCK",
                    AccountType = "Asset",
                    Balance = totalStockCostValue
                });
            }

            // Supplier Payables (Accounts Payable)
            var suppliers = await _supplierRepository.GetAll().AsNoTracking().Where(s => s.AccountId.HasValue).ToListAsync();
            decimal totalSupplierPayables = 0;
            foreach (var supplier in suppliers)
            {
                var bal = await _accountBalanceManager.GetBalanceAsync(supplier.AccountId.Value);
                if (bal < 0)
                {
                    totalSupplierPayables += Math.Abs(bal);
                }
            }

            if (totalSupplierPayables != 0)
            {
                liabilityItems.Add(new BalanceSheetRowDto
                {
                    AccountName = "Accounts Payable (Supplier Payables)",
                    AccountCode = "AP-SUPP",
                    AccountType = "Liability",
                    Balance = totalSupplierPayables
                });
            }

            var assetCategories = new List<BalanceSheetCategoryDto>
            {
                new BalanceSheetCategoryDto
                {
                    CategoryName = "Cash & Liquid Accounts",
                    TotalAmount = cashAndBankItems.Sum(x => x.Balance),
                    Items = cashAndBankItems
                },
                new BalanceSheetCategoryDto
                {
                    CategoryName = "Other Assets & Inventory",
                    TotalAmount = otherAssetItems.Sum(x => x.Balance),
                    Items = otherAssetItems
                }
            };

            var liabilityCategories = new List<BalanceSheetCategoryDto>
            {
                new BalanceSheetCategoryDto
                {
                    CategoryName = "Current Liabilities",
                    TotalAmount = liabilityItems.Sum(x => x.Balance),
                    Items = liabilityItems
                }
            };

            var totalAssets = assetCategories.Sum(c => c.TotalAmount);
            var totalLiabilities = liabilityCategories.Sum(c => c.TotalAmount);

            // Revenue and Expenses for Net Profit calculation
            var salesReport = await GetSaleReportAsync(input);
            var expenseReport = await GetExpenseReportAsync(input);
            var netProfitOrLoss = (salesReport?.TotalAmount ?? 0) - (expenseReport?.TotalAmount ?? 0);

            if (totalInitialCapital > 0 && !equityItems.Any(x => x.AccountCode == "CAP-INIT"))
            {
                equityItems.Add(new BalanceSheetRowDto
                {
                    AccountName = "Initial Owner Capital",
                    AccountCode = "CAP-INIT",
                    AccountType = "Equity",
                    Balance = totalInitialCapital
                });
            }

            equityItems.Add(new BalanceSheetRowDto
            {
                AccountName = "Retained Earnings / Net Profit (Loss)",
                AccountCode = "NET-PROFIT",
                AccountType = "Equity",
                Balance = netProfitOrLoss
            });

            var equityCategories = new List<BalanceSheetCategoryDto>
            {
                new BalanceSheetCategoryDto
                {
                    CategoryName = "Owner Equity & Retained Earnings",
                    TotalAmount = equityItems.Sum(x => x.Balance),
                    Items = equityItems
                }
            };

            var totalEquity = equityCategories.Sum(c => c.TotalAmount);

            return new BalanceSheetReportDto
            {
                TotalAssets = totalAssets,
                TotalLiabilities = totalLiabilities,
                TotalEquity = totalEquity,
                NetProfitOrLoss = netProfitOrLoss,
                InitialCapital = totalInitialCapital,
                IsBalanced = Math.Abs(totalAssets - (totalLiabilities + totalEquity)) < 0.01m,
                AssetCategories = assetCategories,
                LiabilityCategories = liabilityCategories,
                EquityCategories = equityCategories
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

