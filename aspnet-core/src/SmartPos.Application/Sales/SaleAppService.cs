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
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Accounts;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Customers;
using SmartPos.Customers.Dto;
using SmartPos.Inventory;
using SmartPos.Products;
using SmartPos.Products.Dto;
using SmartPos.Sales.Dto;

namespace SmartPos.Sales
{
    [AbpAuthorize(PermissionNames.Pages_Sales)]
    public class SaleAppService : AsyncCrudAppService<Sale, SaleDto, int, PagedSaleResultRequestDto, CreateSaleDto, SaleDto>, ISaleAppService
    {
        private readonly IRepository<SaleLine> _lineRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<LedgerEntry> _ledgerRepository;
        private readonly IRepository<SaleReturn> _saleReturnRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly IBranchStockManager _branchStockManager;
        private readonly IRepository<BranchStock> _branchStockRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<BusinessAccount> _accountRepository;
        private readonly SystemAccountManager _systemAccountManager;

        public SaleAppService(
            IRepository<Sale> repository,
            IRepository<SaleLine> lineRepository,
            IRepository<Product> productRepository,
            IRepository<Customer> customerRepository,
            IRepository<LedgerEntry> ledgerRepository,
            IRepository<SaleReturn> saleReturnRepository,
            IRepository<User, long> userRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            IBranchStockManager branchStockManager,
            IRepository<BranchStock> branchStockRepository,
            IRepository<Branch> branchRepository,
            IRepository<BusinessAccount> accountRepository,
            SystemAccountManager systemAccountManager)
            : base(repository)
        {
            _lineRepository = lineRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _ledgerRepository = ledgerRepository;
            _saleReturnRepository = saleReturnRepository;
            _userRepository = userRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _branchStockManager = branchStockManager;
            _branchStockRepository = branchStockRepository;
            _branchRepository = branchRepository;
            _accountRepository = accountRepository;
            _systemAccountManager = systemAccountManager;
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

        public async Task<ListResultDto<ProductDto>> GetPosProductSuggestionsAsync(string keyword)
        {
            var normalized = keyword.IsNullOrWhiteSpace() ? null : keyword.Trim();
            var query = await GetVisibleProductsQueryAsync();

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

        public async Task<ListResultDto<ProductDto>> GetPosProductsAsync()
        {
            var products = await (await GetVisibleProductsQueryAsync())
                .OrderBy(x => x.Name)
                .Take(1000)
                .ToListAsync();

            return new ListResultDto<ProductDto>(await MapProductsWithBranchStockAsync(products));
        }

        public async Task<ListResultDto<ProductDto>> GetTopSellingProductsAsync(int maxCount = 5)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);
            
            var salesQuery = Repository.GetAll();
            if (branchId.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
            }

            var topProductIds = await _lineRepository.GetAll()
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
                    .Where(p => !existingIds.Contains(p.Id))
                    .OrderByDescending(p => p.CreationTime)
                    .Take(maxCount - topProducts.Count)
                    .ToListAsync();

                topProducts.AddRange(fillProducts);
            }

            return new ListResultDto<ProductDto>(await MapProductsWithBranchStockAsync(topProducts));
        }

        public override async Task<SaleDto> CreateAsync(CreateSaleDto input)
        {
            CheckCreatePermission();
            return await CreateSaleInternalAsync(input);
        }

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

            if (input.SaleDate == default)
            {
                input.SaleDate = Abp.Timing.Clock.Now;
            }

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();
            if (customer.BranchId == 0 || customer.BranchId != branchId)
            {
                customer.BranchId = branchId;
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            var branch = await _branchRepository.GetAsync(branchId);
            var taxPercent = Math.Max(0, branch.TaxPercent);
            var discountPercent = Math.Max(0, branch.DiscountPercent);
            var discountAmountInput = Math.Max(0, branch.DiscountAmount);

            var sale = new Sale
            {
                TenantId = AbpSession.TenantId,
                BranchId = branchId,
                CustomerId = input.CustomerId,
                SaleDate = input.SaleDate,
                Notes = input.Notes,
                DiscountPercent = discountPercent,
                TaxPercent = taxPercent,
                PaymentType = input.PaymentType,
                Lines = new List<SaleLine>()
            };

            decimal subTotal = 0;
            foreach (var lineInput in input.Lines)
            {
                if (lineInput.Quantity <= 0)
                {
                    throw new UserFriendlyException("Quantity must be greater than zero.");
                }

                var product = await _productRepository.GetAsync(lineInput.ProductId);
                await _branchStockManager.EnsureCanUseProductAtBranchAsync(branchId, lineInput.ProductId);
                await _branchStockManager.DecreaseAsync(branchId, lineInput.ProductId, lineInput.Quantity, product.Name);

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
            sale.TotalAmount = Math.Round(taxable + sale.TaxAmount, 2);

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

            foreach (var line in sale.Lines.ToList())
            {
                await _branchStockManager.IncreaseAsync(sale.BranchId, line.ProductId, line.Quantity);
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
            var dto = new SaleDto
            {
                Id = entity.Id,
                CustomerId = entity.CustomerId,
                CustomerName = entity.Customer?.Name,
                SaleDate = entity.SaleDate,
                InvoiceNo = entity.InvoiceNo,
                SubTotal = entity.SubTotal,
                DiscountAmount = entity.DiscountAmount,
                DiscountPercent = entity.DiscountPercent,
                TaxPercent = entity.TaxPercent,
                TaxAmount = entity.TaxAmount,
                TotalAmount = entity.TotalAmount,
                PaymentType = entity.PaymentType,
                CashAmount = entity.CashAmount,
                CardAmount = entity.CardAmount,
                CreditAmount = entity.CreditAmount,
                Notes = entity.Notes,
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
            return dto;
        }

        public override async Task<PagedResultDto<SaleDto>> GetAllAsync(PagedSaleResultRequestDto input)
        {
            var result = await base.GetAllAsync(input);
            await PopulateReturnFlagsAsync(result.Items);
            return result;
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

        private static void ApplyPaymentSplit(Sale sale, CreateSaleDto input)
        {
            var total = sale.TotalAmount;
            switch (input.PaymentType)
            {
                case PaymentTypes.Cash:
                    sale.CashAmount = total;
                    sale.CardAmount = 0;
                    sale.CreditAmount = 0;
                    break;
                case PaymentTypes.Card:
                    sale.CashAmount = 0;
                    sale.CardAmount = total;
                    sale.CreditAmount = 0;
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
