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
using SmartPos.Branches;
using SmartPos.Customers;
using SmartPos.Customers.Dto;
using SmartPos.Products;
using SmartPos.Products.Dto;
using SmartPos.Sales.Dto;
using SmartPos.Inventory;

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
        private readonly SystemAccountManager _systemAccountManager;
        private readonly IBranchStockManager _branchStockManager;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;

        public SaleAppService(
            IRepository<Sale> repository,
            IRepository<SaleLine> lineRepository,
            IRepository<Product> productRepository,
            IRepository<Customer> customerRepository,
            IRepository<LedgerEntry> ledgerRepository,
            IRepository<SaleReturn> saleReturnRepository,
            SystemAccountManager systemAccountManager,
            IBranchStockManager branchStockManager,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext)
            : base(repository)
        {
            _lineRepository = lineRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _ledgerRepository = ledgerRepository;
            _saleReturnRepository = saleReturnRepository;
            _systemAccountManager = systemAccountManager;
            _branchStockManager = branchStockManager;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
        }

        public async Task<ProductDto> GetProductByBarcodeAsync(string barcode)
        {
            return await GetPosProductAsync(barcode);
        }

        public async Task<ProductDto> GetPosProductAsync(string keyword)
        {
            var branchId = RequireBranchContext();

            var normalized = keyword.IsNullOrWhiteSpace() ? null : keyword.Trim();
            if (normalized.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("Enter a barcode or product name.");
            }

            var query = _productRepository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit);

            var byBarcode = await query.FirstOrDefaultAsync(x => x.Barcode == normalized);
            Product product;
            if (byBarcode != null)
            {
                product = byBarcode;
            }
            else
            {
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
                    product = exactName;
                }
                else if (nameMatches.Count == 1)
                {
                    product = nameMatches[0];
                }
                else
                {
                    throw new UserFriendlyException(
                        "Multiple products match \"" + normalized + "\". Pick a more specific name or use the barcode.");
                }
            }

            return await MapPosProductAsync(product, branchId);
        }

        public async Task<ListResultDto<ProductDto>> GetPosProductSuggestionsAsync(string keyword)
        {
            var branchId = RequireBranchContext();

            var normalized = keyword.IsNullOrWhiteSpace() ? null : keyword.Trim();
            var query = _productRepository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit);

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

            var dtos = new List<ProductDto>();
            foreach (var item in items)
            {
                dtos.Add(await MapPosProductAsync(item, branchId));
            }

            return new ListResultDto<ProductDto>(dtos);
        }

        public async Task<ListResultDto<CustomerDto>> GetPosCustomersAsync()
        {
            var customers = await _customerRepository.GetAll()
                .OrderBy(x => x.Name)
                .Take(1000)
                .ToListAsync();

            return new ListResultDto<CustomerDto>(ObjectMapper.Map<List<CustomerDto>>(customers));
        }

        public async Task<ListResultDto<ProductDto>> GetPosProductsAsync()
        {
            var branchId = RequireBranchContext();

            var products = await _productRepository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit)
                .OrderBy(x => x.Name)
                .Take(1000)
                .ToListAsync();

            var dtos = new List<ProductDto>();
            foreach (var product in products)
            {
                dtos.Add(await MapPosProductAsync(product, branchId));
            }

            return new ListResultDto<ProductDto>(dtos);
        }

        public override async Task<SaleDto> CreateAsync(CreateSaleDto input)
        {
            CheckCreatePermission();

            await _branchAccessChecker.EnsureCanAccessAsync(input.BranchId);

            if (input.Lines == null || !input.Lines.Any())
            {
                throw new UserFriendlyException("Add at least one product line.");
            }

            var customer = await _customerRepository.GetAsync(input.CustomerId);
            if (!customer.AccountId.HasValue)
            {
                throw new UserFriendlyException("Customer has no linked account. Open Customers once to create it.");
            }

            if (input.SaleDate == default)
            {
                input.SaleDate = Abp.Timing.Clock.Now;
            }

            var sale = new Sale
            {
                BranchId = input.BranchId,
                CustomerId = input.CustomerId,
                SaleDate = input.SaleDate,
                Notes = input.Notes,
                DiscountPercent = Math.Max(0, input.DiscountPercent),
                TaxPercent = Math.Max(0, input.TaxPercent),
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
                var stockQuantity = await _branchStockManager.GetQuantityAsync(input.BranchId, lineInput.ProductId);
                if (stockQuantity < lineInput.Quantity)
                {
                    throw new UserFriendlyException(
                        $"Insufficient stock for '{product.Name}'. Available: {stockQuantity}, requested: {lineInput.Quantity}.");
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

                await _branchStockManager.DecreaseAsync(input.BranchId, lineInput.ProductId, lineInput.Quantity, product.Name);
            }

            sale.SubTotal = Math.Round(subTotal, 2);

            var discountAmount = input.DiscountAmount;
            if (input.DiscountPercent > 0 && discountAmount <= 0)
            {
                discountAmount = Math.Round(sale.SubTotal * input.DiscountPercent / 100m, 2);
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

            return await GetAsync(new EntityDto<int>(sale.Id));
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

            foreach (var line in sale.Lines.ToList())
            {
                var product = await _productRepository.GetAsync(line.ProductId);
                await _branchStockManager.IncreaseAsync(sale.BranchId, line.ProductId, line.Quantity, product.Name);
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
            return Repository.GetAllIncluding(x => x.Customer, x => x.Branch, x => x.Lines)
                .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.InvoiceNo != null && x.InvoiceNo.Contains(input.Keyword))
                         || (x.Notes != null && x.Notes.Contains(input.Keyword))
                         || (x.Customer != null && x.Customer.Name.Contains(input.Keyword)));
        }

        protected override async Task<Sale> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Customer, x => x.Branch, x => x.Lines)
                    .Where(x => x.Id == id));
        }

        protected override SaleDto MapToEntityDto(Sale entity)
        {
            var dto = new SaleDto
            {
                Id = entity.Id,
                BranchId = entity.BranchId,
                BranchName = entity.Branch?.Name,
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
                Repository.GetAllIncluding(x => x.Customer, x => x.Branch, x => x.Lines)
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

        private int RequireBranchContext()
        {
            var branchId = _branchContext.BranchId;
            if (!branchId.HasValue || branchId.Value <= 0)
            {
                throw new UserFriendlyException("Select a branch.");
            }

            return branchId.Value;
        }

        private async Task<ProductDto> MapPosProductAsync(Product product, int branchId)
        {
            var dto = ObjectMapper.Map<ProductDto>(product);
            dto.StockQuantity = await _branchStockManager.GetQuantityAsync(branchId, product.Id);
            dto.StockProfit = ProductPricing.StockProfit(dto.Price, dto.CostPrice, dto.StockQuantity);
            return dto;
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
