using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Caching;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Accounts;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Caching;
using SmartPos.Customers;
using SmartPos.Customers.Dto;
using SmartPos.Inventory;
using SmartPos.Products;
using SmartPos.Products.Dto;
using SmartPos.Sales.Dto;
using SmartPos.BranchNotificationSettings;
using SmartPos.Orders;
using SmartPos.Authorization.Roles;

namespace SmartPos.Sales
{
    [AbpAuthorize(PermissionNames.Pages_Sales, PermissionNames.Pages_Pos)]
    public class SaleAppService : AsyncCrudAppService<Sale, SaleDto, int, PagedSaleResultRequestDto, CreateSaleDto, SaleDto>, ISaleAppService
    {
        private readonly IRepository<SaleLine> _lineRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<LedgerEntry> _ledgerRepository;
        private readonly IRepository<SaleReturn> _saleReturnRepository;
        private readonly IRepository<CustomerOrder> _customerOrderRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly IBranchStockManager _branchStockManager;
        private readonly IRepository<BranchStock> _branchStockRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<BusinessAccount> _accountRepository;
        private readonly SystemAccountManager _systemAccountManager;
        private readonly IBranchNotificationService _branchNotificationService;
        private readonly ICacheManager _cacheManager;
        private readonly ISmartPosReadCacheInvalidator _readCacheInvalidator;

        public SaleAppService(
            IRepository<Sale> repository,
            IRepository<SaleLine> lineRepository,
            IRepository<Product> productRepository,
            IRepository<Customer> customerRepository,
            IRepository<LedgerEntry> ledgerRepository,
            IRepository<SaleReturn> saleReturnRepository,
            IRepository<CustomerOrder> customerOrderRepository,
            IRepository<User, long> userRepository,
            IRepository<Role> roleRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            IBranchStockManager branchStockManager,
            IRepository<BranchStock> branchStockRepository,
            IRepository<Branch> branchRepository,
            IRepository<BusinessAccount> accountRepository,
            SystemAccountManager systemAccountManager,
            IBranchNotificationService branchNotificationService,
            ICacheManager cacheManager,
            ISmartPosReadCacheInvalidator readCacheInvalidator)
            : base(repository)
        {
            _lineRepository = lineRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _ledgerRepository = ledgerRepository;
            _saleReturnRepository = saleReturnRepository;
            _customerOrderRepository = customerOrderRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _branchStockManager = branchStockManager;
            _branchStockRepository = branchStockRepository;
            _branchRepository = branchRepository;
            _accountRepository = accountRepository;
            _systemAccountManager = systemAccountManager;
            _cacheManager = cacheManager;
            _readCacheInvalidator = readCacheInvalidator;
            _branchNotificationService = branchNotificationService;
            CreatePermissionName = PermissionNames.Pages_Sales_Create;
            UpdatePermissionName = PermissionNames.Pages_Sales_Edit;
            DeletePermissionName = PermissionNames.Pages_Sales_Delete;
            GetPermissionName = PermissionNames.Pages_Sales;
            GetAllPermissionName = PermissionNames.Pages_Sales;
        }

        public async Task<ProductDto> GetProductByBarcodeAsync(string barcode)
        {
            return await GetPosProductAsync(barcode);
        }

        public async Task<ProductDto> GetPosProductAsync(string keyword)
        {
            var normalized = keyword.IsNullOrWhiteSpace() ? null : keyword.Trim();
            if (normalized.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("Enter a barcode or product name.");
            }

            var query = await GetVisibleProductsQueryAsync();

            var byBarcode = await query.FirstOrDefaultAsync(x => x.Barcode == normalized);
            if (byBarcode != null)
            {
                return await MapProductWithBranchStockAsync(byBarcode);
            }

            var lower = normalized.ToLowerInvariant();
            var nameMatches = await query
                .Where(x => x.Name != null && x.Name.ToLower().Contains(lower))
                .OrderBy(x => x.Name)
                .Take(20)
                .ToListAsync();

            if (nameMatches.Count == 0)
            {
                throw new UserFriendlyException("No product found for: " + normalized);
            }

            var exactName = nameMatches.FirstOrDefault(x =>
                string.Equals(x.Name, normalized, StringComparison.OrdinalIgnoreCase));
            if (exactName != null)
            {
                return await MapProductWithBranchStockAsync(exactName);
            }

            if (nameMatches.Count == 1)
            {
                return await MapProductWithBranchStockAsync(nameMatches[0]);
            }

            throw new UserFriendlyException(
                "Multiple products match \"" + normalized + "\". Pick a more specific name or use the barcode.");
        }

        public async Task<ListResultDto<ProductDto>> GetPosProductSuggestionsAsync(string keyword, int? categoryId = null)
        {
            var normalized = keyword.IsNullOrWhiteSpace() ? null : keyword.Trim();
            var query = await GetVisibleProductsQueryAsync();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(x => x.CategoryId == categoryId.Value);
            }

            if (!normalized.IsNullOrWhiteSpace())
            {
                var lower = normalized.ToLowerInvariant();
                query = query.Where(x =>
                    (x.Barcode != null && x.Barcode == normalized) ||
                    (x.Name != null && x.Name.ToLower().Contains(lower)) ||
                    (x.Barcode != null && x.Barcode.ToLower().Contains(lower)));
            }

            var items = await query
                .OrderBy(x => x.Name)
                .Take(50)
                .ToListAsync();

            return new ListResultDto<ProductDto>(await MapProductsWithBranchStockAsync(items));
        }

        public async Task<ListResultDto<CustomerDto>> GetPosCustomersAsync()
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);
            var query = _customerRepository.GetAll();
            if (branchId.HasValue)
            {
                query = query.Where(x => x.BranchId == branchId.Value);
            }

            var customers = await query
                .OrderBy(x => x.Name)
                .Take(1000)
                .ToListAsync();

            return new ListResultDto<CustomerDto>(ObjectMapper.Map<List<CustomerDto>>(customers));
        }

        public async Task<ListResultDto<ProductDto>> GetPosProductsAsync(int? categoryId = null)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);
            var cacheKey = SmartPosAppCaches.TenantBranchKey(
                AbpSession.TenantId,
                branchId,
                $"Cat_{categoryId?.ToString() ?? "All"}");

            var cache = SmartPosAppCaches.Typed<ListResultDto<ProductDto>>(
                _cacheManager,
                SmartPosAppCaches.PosProducts,
                TimeSpan.FromMinutes(2));

            return await cache.GetAsync(cacheKey, async _ =>
            {
                var query = await GetVisibleProductsQueryAsync();
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(x => x.CategoryId == categoryId.Value);
                }

                var products = await query
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Take(1000)
                    .ToListAsync();

                return new ListResultDto<ProductDto>(await MapProductsWithBranchStockAsync(products));
            });
        }

        public async Task<ListResultDto<ProductDto>> GetTopSellingProductsAsync(int maxCount = 5)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);
            var cacheKey = SmartPosAppCaches.TenantBranchKey(AbpSession.TenantId, branchId, $"Top_{maxCount}");
            var cache = SmartPosAppCaches.Typed<ListResultDto<ProductDto>>(
                _cacheManager,
                SmartPosAppCaches.TopSellingProducts,
                TimeSpan.FromMinutes(10));

            return await cache.GetAsync(cacheKey, async _ =>
            {
                var salesQuery = Repository.GetAll().AsNoTracking();
                if (branchId.HasValue)
                {
                    salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
                }

                var topProductIds = await _lineRepository.GetAll()
                    .AsNoTracking()
                    .Where(sl => salesQuery.Any(s => s.Id == sl.SaleId))
                    .GroupBy(sl => sl.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        TotalQuantity = g.Sum(x => x.Quantity)
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(maxCount)
                    .Select(x => x.ProductId)
                    .ToListAsync();

                var visibleQuery = await GetVisibleProductsQueryAsync();
                var topProducts = new List<Product>();

                if (topProductIds.Count > 0)
                {
                    var productsFromSales = await visibleQuery
                        .AsNoTracking()
                        .Where(p => topProductIds.Contains(p.Id))
                        .ToListAsync();

                    topProducts = topProductIds
                        .Select(id => productsFromSales.FirstOrDefault(p => p.Id == id))
                        .Where(p => p != null)
                        .ToList();
                }

                if (topProducts.Count < maxCount)
                {
                    var existingIds = topProducts.Select(p => p.Id).ToList();
                    var fillProducts = await visibleQuery
                        .AsNoTracking()
                        .Where(p => !existingIds.Contains(p.Id))
                        .OrderByDescending(p => p.CreationTime)
                        .Take(maxCount - topProducts.Count)
                        .ToListAsync();

                    topProducts.AddRange(fillProducts);
                }

                return new ListResultDto<ProductDto>(await MapProductsWithBranchStockAsync(topProducts));
            });
        }

        public override async Task<SaleDto> CreateAsync(CreateSaleDto input)
        {
            CheckCreatePermission();
            return await CreateSaleInternalAsync(input);
        }

        [AbpAllowAnonymous]
        [RemoteService(false)]
        public async Task<SaleDto> CreateSaleInternalAsync(CreateSaleDto input)
        {
            if (input.Lines == null || !input.Lines.Any())
            {
                throw new UserFriendlyException("Add at least one product line.");
            }

            var customer = await _customerRepository.GetAsync(input.CustomerId);
            if (!customer.AccountId.HasValue)
            {
                var account = new BusinessAccount
                {
                    TenantId = AbpSession.TenantId,
                    Name = customer.Name,
                    Code = "CUS-" + customer.Id,
                    AccountType = AccountTypes.Customer,
                    OpeningBalance = 0,
                    Description = "Customer account: " + customer.Name,
                    IsActive = true
                };
                customer.AccountId = await _accountRepository.InsertAndGetIdAsync(account);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            var now = Abp.Timing.Clock.Now;
            if (input.SaleDate == default)
            {
                input.SaleDate = now;
            }
            else if (input.SaleDate.TimeOfDay == TimeSpan.Zero)
            {
                input.SaleDate = input.SaleDate.Date == now.Date
                    ? now
                    : input.SaleDate.Date.Add(now.TimeOfDay);
            }

            var branchId = input.BranchId.HasValue && input.BranchId.Value > 0
                ? input.BranchId.Value
                : await _branchAccessChecker.RequireEffectiveBranchIdAsync();

            if (customer.BranchId == 0 || customer.BranchId != branchId)
            {
                customer.BranchId = branchId;
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            var branch = await _branchRepository.GetAsync(branchId);
            var tenantId = branch.TenantId ?? AbpSession.TenantId;

            if (branch.BusinessType == BusinessType.Restaurant && AbpSession.UserId.HasValue)
            {
                var user = await _userRepository.GetAllIncluding(u => u.Roles).FirstOrDefaultAsync(u => u.Id == AbpSession.UserId.Value);
                if (user != null && user.Roles.Any())
                {
                    var roleIds = user.Roles.Select(r => r.RoleId).ToList();
                    var isWaiter = await _roleRepository.GetAll().AnyAsync(r => roleIds.Contains(r.Id) && r.Name == StaticRoleNames.Tenants.Waiter);
                    if (isWaiter)
                    {
                        throw new UserFriendlyException("Waiters are not authorized to settle payments or complete sales.");
                    }
                }
            }

            var taxPercent = Math.Max(0, branch.TaxPercent);
            var discountPercent = Math.Max(0, branch.DiscountPercent);
            var discountAmountInput = Math.Max(0, branch.DiscountAmount);

            var sale = new Sale
            {
                TenantId = tenantId,
                BranchId = branchId,
                CustomerId = input.CustomerId,
                SaleDate = input.SaleDate,
                Notes = input.Notes,
                TokenNo = input.TokenNo,
                DiscountPercent = discountPercent,
                TaxPercent = taxPercent,
                PaymentType = input.PaymentType,
                DeliveryCharges = Math.Max(0, input.DeliveryCharges),
                DeliveryAreaName = input.DeliveryAreaName,
                Lines = new List<SaleLine>()
            };

            decimal subTotal = 0;
            foreach (var lineInput in input.Lines)
            {
                if (lineInput.Quantity <= 0)
                {
                    throw new UserFriendlyException("Quantity must be greater than zero.");
                }

                var product = await _productRepository.GetAllIncluding(x => x.Category)
                    .FirstOrDefaultAsync(p => p.Id == lineInput.ProductId);
                if (product == null)
                {
                    throw new UserFriendlyException("Product not found.");
                }
                if ((product.Barcode != null && product.Barcode.StartsWith("GROCERY-"))
                    || product.Name == "Grocery"
                    || (product.Category != null && product.Category.Name == "Permanent Grocery"))
                {
                    throw new UserFriendlyException("Grocery items cannot be sold in Sales.");
                }

                if (branch.BusinessType != BusinessType.Restaurant)
                {
                    await _branchStockManager.EnsureCanUseProductAtBranchAsync(branchId, lineInput.ProductId);
                    await _branchStockManager.DecreaseAsync(branchId, lineInput.ProductId, lineInput.Quantity, product.Name);
                }

                var lineTotal = lineInput.Quantity * lineInput.UnitPrice;
                subTotal += lineTotal;

                sale.Lines.Add(new SaleLine
                {
                    ProductId = lineInput.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitPrice = lineInput.UnitPrice,
                    LineTotal = lineTotal
                });
            }

            sale.SubTotal = Math.Round(subTotal, 2);

            var discountAmount = discountAmountInput;
            if (discountPercent > 0 && discountAmount <= 0)
            {
                discountAmount = Math.Round(sale.SubTotal * discountPercent / 100m, 2);
            }

            if (discountAmount < 0)
            {
                discountAmount = 0;
            }

            if (discountAmount > sale.SubTotal)
            {
                discountAmount = sale.SubTotal;
            }

            sale.DiscountAmount = discountAmount;
            var taxable = sale.SubTotal - sale.DiscountAmount;
            sale.TaxAmount = Math.Round(taxable * sale.TaxPercent / 100m, 2);
            sale.TotalAmount = Math.Round(taxable + sale.TaxAmount + sale.DeliveryCharges, 2);

            ApplyPaymentSplit(sale, input);

            await Repository.InsertAsync(sale);
            await CurrentUnitOfWork.SaveChangesAsync();

            sale.InvoiceNo = "SAL-" + sale.Id.ToString("D6");
            await PostSaleLedgerAsync(sale, customer.AccountId.Value);
            await CurrentUnitOfWork.SaveChangesAsync();

            sale.Customer = customer;
            foreach (var line in sale.Lines)
            {
                line.Product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
            }

            var dto = MapToEntityDto(sale);
            await PopulateReturnFlagsAsync(new[] { dto });

            _branchNotificationService.SendNotificationAfterCommit(
                BranchNotificationType.Sale,
                sale.TenantId,
                sale.BranchId,
                sale.InvoiceNo,
                sale.TotalAmount,
                AbpSession.UserId,
                sale.SaleDate);

            // Stock/top-sellers change after sale — refresh hot-path caches.
            await SmartPosAppCaches.ClearManyAsync(
                _cacheManager,
                SmartPosAppCaches.TopSellingProducts,
                SmartPosAppCaches.PosProducts,
                SmartPosAppCaches.OnlineCatalog);

            return dto;
        }

        public override async Task<SaleDto> UpdateAsync(SaleDto input)
        {
            throw new UserFriendlyException("Sales cannot be edited. Delete and create a new sale instead.");
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var sale = await GetEntityByIdAsync(input.Id);
            if (sale == null)
            {
                throw new UserFriendlyException("Sale not found.");
            }

            var hasReturns = await _saleReturnRepository.CountAsync(x => x.SaleId == sale.Id) > 0;
            if (hasReturns)
            {
                throw new UserFriendlyException("Cannot delete this sale because it has product returns. Delete the returns first.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(sale.BranchId);

            var branch = await _branchRepository.FirstOrDefaultAsync(sale.BranchId);
            foreach (var line in sale.Lines.ToList())
            {
                if (branch == null || branch.BusinessType != BusinessType.Restaurant)
                {
                    await _branchStockManager.IncreaseAsync(sale.BranchId, line.ProductId, line.Quantity);
                }
                await _lineRepository.DeleteAsync(line);
            }

            var ledgerEntries = await _ledgerRepository.GetAllListAsync(
                x => x.VoucherType == VoucherTypes.Sale && x.VoucherId == sale.Id);
            foreach (var entry in ledgerEntries)
            {
                await _ledgerRepository.DeleteAsync(entry);
            }

            await Repository.DeleteAsync(sale);
        }

        protected override IQueryable<Sale> CreateFilteredQuery(PagedSaleResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            return Repository.GetAllIncluding(x => x.Customer, x => x.Lines)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.InvoiceNo != null && x.InvoiceNo.Contains(input.Keyword))
                         || (x.Notes != null && x.Notes.Contains(input.Keyword))
                         || (x.Customer != null && x.Customer.Name.Contains(input.Keyword)));
        }

        private async Task<IQueryable<Product>> GetVisibleProductsQueryAsync()
        {
            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();
            return _productRepository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit)
                .Where(p => (p.Barcode == null || !p.Barcode.StartsWith("GROCERY-")) && p.Name != "Grocery")
                .Where(p => p.Category == null || p.Category.Name != "Permanent Grocery")
                .WhereVisibleToBranch(_branchStockRepository.GetAll(), branchId);
        }

        private async Task<ProductDto> MapProductWithBranchStockAsync(Product product)
        {
            var dtos = await MapProductsWithBranchStockAsync(new[] { product });
            return dtos[0];
        }

        private async Task<List<ProductDto>> MapProductsWithBranchStockAsync(IList<Product> products)
        {
            var dtos = ObjectMapper.Map<List<ProductDto>>(products);
            if (dtos.Count == 0)
            {
                return dtos;
            }

            var branchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();
            if (!branchId.HasValue)
            {
                return dtos;
            }

            var infoMap = await _branchStockManager.GetBranchProductInfoAsync(
                branchId.Value,
                products.Select(x => x.Id));

            foreach (var dto in dtos)
            {
                if (infoMap.TryGetValue(dto.Id, out var info))
                {
                    dto.StockQuantity = info.Quantity;
                    dto.Price = info.Price;
                    dto.WholesalePrice = info.WholesalePrice;
                    dto.CostPrice = info.CostPrice;
                }
                else
                {
                    dto.StockQuantity = 0;
                }

                dto.StockProfit = ProductPricing.StockProfit(dto.Price, dto.CostPrice, dto.StockQuantity);
            }

            return dtos;
        }

        protected override async Task<Sale> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Customer, x => x.Lines)
                    .Where(x => x.Id == id));
        }

        protected override SaleDto MapToEntityDto(Sale entity)
        {
            var paidAmount = Math.Round(entity.CashAmount + entity.CardAmount, 2);
            var dueAmount = Math.Round(Math.Max(0m, entity.TotalAmount - paidAmount), 2);
            var dto = new SaleDto
            {
                Id = entity.Id,
                CustomerId = entity.CustomerId,
                CustomerName = entity.Customer?.Name,
                SaleDate = entity.SaleDate,
                InvoiceNo = entity.InvoiceNo,
                TokenNo = entity.TokenNo,
                SubTotal = entity.SubTotal,
                DiscountAmount = entity.DiscountAmount,
                DiscountPercent = entity.DiscountPercent,
                TaxPercent = entity.TaxPercent,
                TaxAmount = entity.TaxAmount,
                DeliveryCharges = entity.DeliveryCharges,
                DeliveryAreaName = entity.DeliveryAreaName,
                TotalAmount = entity.TotalAmount,
                PaymentType = entity.PaymentType,
                CashAmount = entity.CashAmount,
                CardAmount = entity.CardAmount,
                CreditAmount = entity.CreditAmount,
                PaidAmount = paidAmount,
                DueAmount = dueAmount,
                SaleType = SaleTypeResolver.Resolve(entity.Notes, entity.DeliveryCharges, entity.DeliveryAreaName),
                Notes = entity.Notes,
                PaymentHistory = new List<SalePaymentHistoryItemDto>(),
                Lines = new List<SaleLineDto>()
            };

            if (entity.Lines != null)
            {
                foreach (var line in entity.Lines)
                {
                    dto.Lines.Add(new SaleLineDto
                    {
                        Id = line.Id,
                        SaleId = line.SaleId,
                        ProductId = line.ProductId,
                        ProductName = line.Product?.Name,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        LineTotal = line.LineTotal
                    });
                }
            }

            return dto;
        }

        public override async Task<SaleDto> GetAsync(EntityDto<int> input)
        {
            var entity = await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Customer, x => x.Lines)
                    .Where(x => x.Id == input.Id));

            if (entity == null)
            {
                throw new UserFriendlyException("Sale not found.");
            }

            foreach (var line in entity.Lines)
            {
                line.Product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
            }

            var dto = MapToEntityDto(entity);
            await PopulateReturnFlagsAsync(new[] { dto });
            await PopulateSaleTypesAsync(new[] { dto });
            await PopulatePaymentHistoryAsync(dto);
            return dto;
        }

        /// <summary>
        /// Builds payment history from ledger cash/bank debits for this sale
        /// (original sale tender + later CollectPayment rows). Display-only.
        /// </summary>
        private async Task PopulatePaymentHistoryAsync(SaleDto dto)
        {
            if (dto == null)
            {
                return;
            }

            var entries = await _ledgerRepository.GetAll()
                .AsNoTracking()
                .Where(x =>
                    x.VoucherId == dto.Id &&
                    x.Debit > 0 &&
                    (x.VoucherType == VoucherTypes.Sale || x.VoucherType == VoucherTypes.SalePayment))
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var history = new List<SalePaymentHistoryItemDto>();
            foreach (var entry in entries)
            {
                var desc = entry.Description ?? string.Empty;
                // Skip AR / credit receivable postings — only cash & card receipts.
                if (desc.IndexOf("(Credit)", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                var method = "Cash";
                if (desc.IndexOf("(Card)", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    desc.IndexOf("via Card", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    method = "Card";
                }
                else if (desc.IndexOf("(Cash)", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         desc.IndexOf("via Cash", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    method = "Cash";
                }

                history.Add(new SalePaymentHistoryItemDto
                {
                    PaymentDate = entry.TransactionDate,
                    Amount = entry.Debit,
                    Method = method,
                    Description = desc,
                    IsAdditionalCollection = string.Equals(
                        entry.VoucherType,
                        VoucherTypes.SalePayment,
                        StringComparison.OrdinalIgnoreCase)
                });
            }

            // Fallback when ledger has no cash/card rows but sale totals show payments
            // (e.g. older data before ledger posting, or notes-only collections).
            if (history.Count == 0 && (dto.CashAmount > 0 || dto.CardAmount > 0))
            {
                if (dto.CashAmount > 0)
                {
                    history.Add(new SalePaymentHistoryItemDto
                    {
                        PaymentDate = dto.SaleDate,
                        Amount = dto.CashAmount,
                        Method = "Cash",
                        Description = $"Sale {dto.InvoiceNo} (Cash)",
                        IsAdditionalCollection = false
                    });
                }

                if (dto.CardAmount > 0)
                {
                    history.Add(new SalePaymentHistoryItemDto
                    {
                        PaymentDate = dto.SaleDate,
                        Amount = dto.CardAmount,
                        Method = "Card",
                        Description = $"Sale {dto.InvoiceNo} (Card)",
                        IsAdditionalCollection = false
                    });
                }
            }

            dto.PaymentHistory = history;
        }

        public override async Task<PagedResultDto<SaleDto>> GetAllAsync(PagedSaleResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);
            await SyncDeliveredCustomerOrderSalesAsync(branchId);
            var result = await base.GetAllAsync(input);
            await PopulateReturnFlagsAsync(result.Items);
            await PopulateSaleTypesAsync(result.Items);
            return result;
        }

        [AbpAllowAnonymous]
        [RemoteService(false)]
        public async Task<SaleDto> CreateSaleForCustomerOrderAsync(int orderId, decimal? paymentReceivedAmount = null)
        {
            var order = await _customerOrderRepository.GetAllIncluding(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                throw new UserFriendlyException("Customer order not found.");
            }

            // Until the order is successfully delivered, do not create or add to any sale
            if (order.Status != CustomerOrderStatus.Delivered)
            {
                return null;
            }

            if (order.SaleId.HasValue && order.SaleId.Value > 0)
            {
                var existing = await Repository.FirstOrDefaultAsync(order.SaleId.Value);
                if (existing != null)
                {
                    if (paymentReceivedAmount.HasValue)
                    {
                        var rec = Math.Max(0m, paymentReceivedAmount.Value);
                        existing.CashAmount = Math.Min(existing.TotalAmount, rec);
                        existing.CardAmount = 0;
                        existing.CreditAmount = Math.Max(0m, existing.TotalAmount - existing.CashAmount);
                        existing.PaymentType = existing.CreditAmount <= 0 ? PaymentTypes.Cash : (existing.CashAmount > 0 ? PaymentTypes.Mixed : PaymentTypes.Credit);
                        await Repository.UpdateAsync(existing);
                        await CurrentUnitOfWork.SaveChangesAsync();
                    }

                    var existingDto = MapToEntityDto(existing);
                    existingDto.SaleType = SaleTypeResolver.OnlineOrder;
                    return existingDto;
                }
            }

            if (order.Lines == null || !order.Lines.Any())
            {
                return null;
            }

            var orderNoLabel = !string.IsNullOrWhiteSpace(order.OrderNo) ? order.OrderNo : $"#{order.Id}";
            var notePrefix = $"[Online Order - {orderNoLabel}]";
            var combinedNotes = string.IsNullOrWhiteSpace(order.Notes)
                ? notePrefix
                : (order.Notes.Contains(notePrefix) ? order.Notes : $"{notePrefix} {order.Notes.Trim()}");

            if (combinedNotes.Length > Sale.MaxNotesLength)
            {
                combinedNotes = combinedNotes.Substring(0, Sale.MaxNotesLength);
            }

            var saleDate = order.LastModificationTime.HasValue
                ? order.LastModificationTime.Value
                : (order.OrderDate != default ? order.OrderDate : Abp.Timing.Clock.Now);

            var total = order.TotalAmount;
            var received = paymentReceivedAmount.HasValue ? Math.Max(0m, paymentReceivedAmount.Value) : total;
            var cash = Math.Min(total, received);
            var credit = Math.Max(0m, total - cash);
            var paymentType = credit <= 0 ? PaymentTypes.Cash : (cash > 0 ? PaymentTypes.Mixed : PaymentTypes.Credit);

            var saleDto = await CreateSaleInternalAsync(new CreateSaleDto
            {
                BranchId = order.BranchId,
                CustomerId = order.CustomerId,
                SaleDate = saleDate,
                PaymentType = paymentType,
                CashAmount = cash,
                CreditAmount = credit,
                DeliveryCharges = order.DeliveryFee,
                DeliveryAreaName = order.DeliveryAreaName,
                Notes = combinedNotes,
                Lines = order.Lines.Select(l => new CreateSaleLineDto
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                }).ToList()
            });

            order.SaleId = saleDto.Id;
            await _customerOrderRepository.UpdateAsync(order);
            await CurrentUnitOfWork.SaveChangesAsync();

            saleDto.SaleType = SaleTypeResolver.OnlineOrder;
            saleDto.PaidAmount = Math.Round(cash, 2);
            saleDto.DueAmount = Math.Round(credit, 2);
            return saleDto;
        }

        [AbpAllowAnonymous]
        [RemoteService(false)]
        public async Task SyncDeliveredCustomerOrderSalesAsync(int? branchId = null)
        {
            // 1. Cleanup: If any order is NOT delivered (Pending, Approved, InProgress, Rejected)
            // but currently has a SaleId linked, unlink and delete that premature sale so it does
            // not appear on Sale page, Report, or Dashboard!
            var unearnedSales = await _customerOrderRepository.GetAll()
                .Where(o => o.Status != CustomerOrderStatus.Delivered && o.SaleId != null)
                .WhereIf(branchId.HasValue && branchId.Value > 0, o => o.BranchId == branchId.Value)
                .ToListAsync();

            foreach (var order in unearnedSales)
            {
                try
                {
                    var saleIdToDelete = order.SaleId.Value;
                    order.SaleId = null;
                    await _customerOrderRepository.UpdateAsync(order);
                    await CurrentUnitOfWork.SaveChangesAsync();

                    var existingSale = await Repository.FirstOrDefaultAsync(saleIdToDelete);
                    if (existingSale != null)
                    {
                        await Repository.DeleteAsync(existingSale);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to cleanup premature sale for undelivered order #{order.Id}: {ex.Message}");
                }
            }

            // 2. Sync: Ensure all successfully DELIVERED orders have a corresponding Sale record
            var deliveredOrders = await _customerOrderRepository.GetAllIncluding(o => o.Lines)
                .Where(o => o.Status == CustomerOrderStatus.Delivered && o.SaleId == null)
                .WhereIf(branchId.HasValue && branchId.Value > 0, o => o.BranchId == branchId.Value)
                .ToListAsync();

            foreach (var order in deliveredOrders)
            {
                try
                {
                    await CreateSaleForCustomerOrderAsync(order.Id);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to auto-create sale for online order #{order.Id}: {ex.Message}");
                }
            }
        }

        private async Task PopulateSaleTypesAsync(IReadOnlyList<SaleDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var saleIds = items.Select(x => x.Id).ToList();
            var onlineOrderSaleIds = await _customerOrderRepository.GetAll()
                .AsNoTracking()
                .Where(o => o.SaleId.HasValue && saleIds.Contains(o.SaleId.Value))
                .Select(o => o.SaleId.Value)
                .ToListAsync();

            var onlineSet = new HashSet<int>(onlineOrderSaleIds);

            foreach (var item in items)
            {
                item.PaidAmount = Math.Round(item.CashAmount + item.CardAmount, 2);
                item.DueAmount = Math.Round(Math.Max(0m, item.TotalAmount - item.PaidAmount), 2);

                if (onlineSet.Contains(item.Id))
                {
                    item.SaleType = SaleTypeResolver.OnlineOrder;
                }
                else
                {
                    item.SaleType = SaleTypeResolver.Resolve(item.Notes, item.DeliveryCharges, item.DeliveryAreaName);
                }
            }
        }

        private async Task PopulateReturnFlagsAsync(IReadOnlyList<SaleDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var saleIds = items.Select(x => x.Id).ToList();
            var counts = await _saleReturnRepository.GetAll()
                .Where(x => saleIds.Contains(x.SaleId))
                .GroupBy(x => x.SaleId)
                .Select(g => new { SaleId = g.Key, Count = g.Count() })
                .ToListAsync();

            var bySaleId = counts.ToDictionary(x => x.SaleId, x => x.Count);
            foreach (var item in items)
            {
                if (bySaleId.TryGetValue(item.Id, out var count) && count > 0)
                {
                    item.ReturnCount = count;
                    item.HasReturns = true;
                }
            }
        }

        public async Task<SaleDto> CollectPaymentAsync(CollectSalePaymentDto input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Amount <= 0)
            {
                throw new UserFriendlyException("Payment amount must be greater than zero.");
            }

            var sale = await Repository.GetAllIncluding(s => s.Customer, s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == input.SaleId);

            if (sale == null)
            {
                throw new UserFriendlyException("Sale not found.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(sale.BranchId);

            var currentPaid = sale.CashAmount + sale.CardAmount;
            var currentDue = Math.Max(0m, sale.TotalAmount - currentPaid);

            if (currentDue <= 0.001m)
            {
                throw new UserFriendlyException("This sale is already fully paid. No pending due amount.");
            }

            if (input.Amount > currentDue + 0.01m)
            {
                throw new UserFriendlyException($"Collected amount ({input.Amount:N2}) exceeds remaining due amount ({currentDue:N2}).");
            }

            var payAmount = Math.Min(input.Amount, currentDue);
            var isCard = input.PaymentMethod == PaymentTypes.Card;

            if (isCard)
            {
                sale.CardAmount += payAmount;
            }
            else
            {
                sale.CashAmount += payAmount;
            }

            sale.CreditAmount = Math.Max(0m, sale.TotalAmount - (sale.CashAmount + sale.CardAmount));

            if (sale.CreditAmount <= 0.001m)
            {
                sale.CreditAmount = 0;
                if (sale.PaymentType == PaymentTypes.Credit)
                {
                    sale.PaymentType = isCard ? PaymentTypes.Card : PaymentTypes.Cash;
                }
                else if (sale.CashAmount > 0 && sale.CardAmount > 0)
                {
                    sale.PaymentType = PaymentTypes.Mixed;
                }
            }
            else
            {
                sale.PaymentType = PaymentTypes.Mixed;
            }

            var methodLabel = isCard ? "Card" : "Cash";
            var paymentNote = $"[Collected PKR {payAmount:N2} via {methodLabel}" + (!string.IsNullOrWhiteSpace(input.Notes) ? $": {input.Notes.Trim()}" : "") + "]";
            sale.Notes = string.IsNullOrWhiteSpace(sale.Notes) ? paymentNote : $"{sale.Notes} | {paymentNote}";
            if (sale.Notes.Length > Sale.MaxNotesLength)
            {
                sale.Notes = sale.Notes.Substring(0, Sale.MaxNotesLength);
            }

            await Repository.UpdateAsync(sale);

            // Post Ledger Entries
            var now = Abp.Timing.Clock.Now;
            var description = $"Payment collected for Sale {sale.InvoiceNo}";
            var paymentAccount = isCard
                ? await _systemAccountManager.GetBankAccountAsync()
                : await _systemAccountManager.GetCashAccountAsync();

            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                TenantId = AbpSession.TenantId,
                BranchId = sale.BranchId,
                AccountId = paymentAccount.Id,
                TransactionDate = now,
                VoucherType = VoucherTypes.SalePayment,
                VoucherId = sale.Id,
                Debit = payAmount,
                Credit = 0,
                Description = $"{description} ({methodLabel})"
            });

            if (sale.Customer?.AccountId.HasValue == true)
            {
                await _ledgerRepository.InsertAsync(new LedgerEntry
                {
                    TenantId = AbpSession.TenantId,
                    BranchId = sale.BranchId,
                    AccountId = sale.Customer.AccountId.Value,
                    TransactionDate = now,
                    VoucherType = VoucherTypes.SalePayment,
                    VoucherId = sale.Id,
                    Debit = 0,
                    Credit = payAmount,
                    Description = $"{description} (AR reduction)"
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            _readCacheInvalidator?.ClearAllHotPathAsync();

            var dto = MapToEntityDto(sale);
            await PopulateReturnFlagsAsync(new[] { dto });
            await PopulateSaleTypesAsync(new[] { dto });
            await PopulatePaymentHistoryAsync(dto);
            return dto;
        }

        private static void ApplyPaymentSplit(Sale sale, CreateSaleDto input)
        {
            var total = sale.TotalAmount;
            switch (input.PaymentType)
            {
                case PaymentTypes.Cash:
                    if (input.CashAmount > 0 && input.CashAmount < total)
                    {
                        sale.CashAmount = Math.Round(input.CashAmount, 2);
                        sale.CardAmount = 0;
                        sale.CreditAmount = Math.Round(total - sale.CashAmount, 2);
                        sale.PaymentType = PaymentTypes.Mixed;
                    }
                    else
                    {
                        sale.CashAmount = total;
                        sale.CardAmount = 0;
                        sale.CreditAmount = 0;
                    }
                    break;
                case PaymentTypes.Card:
                    if (input.CardAmount > 0 && input.CardAmount < total)
                    {
                        sale.CashAmount = 0;
                        sale.CardAmount = Math.Round(input.CardAmount, 2);
                        sale.CreditAmount = Math.Round(total - sale.CardAmount, 2);
                        sale.PaymentType = PaymentTypes.Mixed;
                    }
                    else
                    {
                        sale.CashAmount = 0;
                        sale.CardAmount = total;
                        sale.CreditAmount = 0;
                    }
                    break;
                case PaymentTypes.Mixed:
                    sale.CashAmount = Math.Max(0, input.CashAmount);
                    sale.CardAmount = Math.Max(0, input.CardAmount);
                    var paid = sale.CashAmount + sale.CardAmount;
                    if (paid > total)
                    {
                        throw new UserFriendlyException("Cash + card amount cannot exceed sale total.");
                    }

                    sale.CreditAmount = Math.Round(total - paid, 2);
                    break;
                case PaymentTypes.Credit:
                default:
                    sale.PaymentType = PaymentTypes.Credit;
                    sale.CashAmount = 0;
                    sale.CardAmount = 0;
                    sale.CreditAmount = total;
                    break;
            }
        }

        private async Task PostSaleLedgerAsync(Sale sale, int customerAccountId)
        {
            var saleAccount = await _systemAccountManager.GetSaleAccountAsync();
            var description = "Sale " + sale.InvoiceNo;

            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                TenantId = AbpSession.TenantId,
                BranchId = sale.BranchId,
                AccountId = saleAccount.Id,
                TransactionDate = sale.SaleDate,
                VoucherType = VoucherTypes.Sale,
                VoucherId = sale.Id,
                Debit = 0,
                Credit = sale.TotalAmount,
                Description = description
            });

            if (sale.CashAmount > 0)
            {
                var cash = await _systemAccountManager.GetCashAccountAsync();
                await _ledgerRepository.InsertAsync(new LedgerEntry
                {
                    TenantId = AbpSession.TenantId,
                    BranchId = sale.BranchId,
                    AccountId = cash.Id,
                    TransactionDate = sale.SaleDate,
                    VoucherType = VoucherTypes.Sale,
                    VoucherId = sale.Id,
                    Debit = sale.CashAmount,
                    Credit = 0,
                    Description = description + " (Cash)"
                });
            }

            if (sale.CardAmount > 0)
            {
                var bank = await _systemAccountManager.GetBankAccountAsync();
                await _ledgerRepository.InsertAsync(new LedgerEntry
                {
                    TenantId = AbpSession.TenantId,
                    BranchId = sale.BranchId,
                    AccountId = bank.Id,
                    TransactionDate = sale.SaleDate,
                    VoucherType = VoucherTypes.Sale,
                    VoucherId = sale.Id,
                    Debit = sale.CardAmount,
                    Credit = 0,
                    Description = description + " (Card)"
                });
            }

            if (sale.CreditAmount > 0)
            {
                await _ledgerRepository.InsertAsync(new LedgerEntry
                {
                    TenantId = AbpSession.TenantId,
                    BranchId = sale.BranchId,
                    AccountId = customerAccountId,
                    TransactionDate = sale.SaleDate,
                    VoucherType = VoucherTypes.Sale,
                    VoucherId = sale.Id,
                    Debit = sale.CreditAmount,
                    Credit = 0,
                    Description = description + " (Credit)"
                });
            }
        }
    }
}
