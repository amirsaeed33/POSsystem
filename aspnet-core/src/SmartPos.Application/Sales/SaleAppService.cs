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
using SmartPos.Accounts;
using SmartPos.Authorization;
using SmartPos.Customers;
using SmartPos.Products;
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
        private readonly SystemAccountManager _systemAccountManager;

        public SaleAppService(
            IRepository<Sale> repository,
            IRepository<SaleLine> lineRepository,
            IRepository<Product> productRepository,
            IRepository<Customer> customerRepository,
            IRepository<LedgerEntry> ledgerRepository,
            IRepository<SaleReturn> saleReturnRepository,
            SystemAccountManager systemAccountManager)
            : base(repository)
        {
            _lineRepository = lineRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _ledgerRepository = ledgerRepository;
            _saleReturnRepository = saleReturnRepository;
            _systemAccountManager = systemAccountManager;
        }

        public override async Task<SaleDto> CreateAsync(CreateSaleDto input)
        {
            CheckCreatePermission();

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
                CustomerId = input.CustomerId,
                SaleDate = input.SaleDate,
                Notes = input.Notes,
                Lines = new List<SaleLine>()
            };

            decimal total = 0;
            foreach (var lineInput in input.Lines)
            {
                if (lineInput.Quantity <= 0)
                {
                    throw new UserFriendlyException("Quantity must be greater than zero.");
                }

                var product = await _productRepository.GetAsync(lineInput.ProductId);
                if (product.StockQuantity < lineInput.Quantity)
                {
                    throw new UserFriendlyException(
                        $"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}, requested: {lineInput.Quantity}.");
                }

                var lineTotal = lineInput.Quantity * lineInput.UnitPrice;
                total += lineTotal;

                sale.Lines.Add(new SaleLine
                {
                    ProductId = lineInput.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitPrice = lineInput.UnitPrice,
                    LineTotal = lineTotal
                });

                product.StockQuantity -= lineInput.Quantity;
            }

            sale.TotalAmount = total;

            await Repository.InsertAsync(sale);
            await CurrentUnitOfWork.SaveChangesAsync();

            sale.InvoiceNo = "SAL-" + sale.Id.ToString("D6");

            var saleAccount = await _systemAccountManager.GetSaleAccountAsync();
            var description = "Sale " + sale.InvoiceNo;

            // Debit Customer (AR), Credit Sale account
            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = customer.AccountId.Value,
                TransactionDate = sale.SaleDate,
                VoucherType = VoucherTypes.Sale,
                VoucherId = sale.Id,
                Debit = sale.TotalAmount,
                Credit = 0,
                Description = description
            });

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
                product.StockQuantity += line.Quantity;
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
            return Repository.GetAllIncluding(x => x.Customer, x => x.Lines)
                .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.InvoiceNo != null && x.InvoiceNo.Contains(input.Keyword))
                         || (x.Notes != null && x.Notes.Contains(input.Keyword))
                         || (x.Customer != null && x.Customer.Name.Contains(input.Keyword)));
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
                TotalAmount = entity.TotalAmount,
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

            return MapToEntityDto(entity);
        }
    }
}
