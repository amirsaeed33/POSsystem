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
using SmartPos.Products;
using SmartPos.Sales.Dto;

namespace SmartPos.Sales
{
    [AbpAuthorize(PermissionNames.Pages_Sales)]
    public class SaleReturnAppService : ApplicationService, ISaleReturnAppService
    {
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<SaleReturn> _returnRepository;
        private readonly IRepository<SaleReturnLine> _returnLineRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<LedgerEntry> _ledgerRepository;
        private readonly SystemAccountManager _systemAccountManager;

        public SaleReturnAppService(
            IRepository<Sale> saleRepository,
            IRepository<SaleReturn> returnRepository,
            IRepository<SaleReturnLine> returnLineRepository,
            IRepository<Product> productRepository,
            IRepository<LedgerEntry> ledgerRepository,
            SystemAccountManager systemAccountManager)
        {
            _saleRepository = saleRepository;
            _returnRepository = returnRepository;
            _returnLineRepository = returnLineRepository;
            _productRepository = productRepository;
            _ledgerRepository = ledgerRepository;
            _systemAccountManager = systemAccountManager;
        }

        public async Task<SaleReturnableDto> GetReturnableSaleAsync(EntityDto<int> input)
        {
            var sale = await _saleRepository.GetAllIncluding(x => x.Customer, x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == input.Id);

            if (sale == null)
            {
                throw new UserFriendlyException("Sale not found.");
            }

            var returnedByLine = await GetReturnedQuantitiesBySaleLineAsync(sale.Id);
            var lines = new List<SaleReturnableLineDto>();

            foreach (var line in sale.Lines.OrderBy(x => x.Id))
            {
                line.Product ??= await _productRepository.FirstOrDefaultAsync(line.ProductId);
                returnedByLine.TryGetValue(line.Id, out var returnedQty);
                var returnable = line.Quantity - returnedQty;
                if (returnable <= 0)
                {
                    continue;
                }

                lines.Add(new SaleReturnableLineDto
                {
                    Id = line.Id,
                    SaleLineId = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name,
                    SoldQuantity = line.Quantity,
                    ReturnedQuantity = returnedQty,
                    ReturnableQuantity = returnable,
                    UnitPrice = line.UnitPrice
                });
            }

            return new SaleReturnableDto
            {
                Id = sale.Id,
                CustomerId = sale.CustomerId,
                CustomerName = sale.Customer?.Name,
                InvoiceNo = sale.InvoiceNo,
                SaleDate = sale.SaleDate,
                Lines = lines
            };
        }

        public async Task<SaleReturnDto> CreateAsync(CreateSaleReturnDto input)
        {
            if (input.Lines == null || !input.Lines.Any(x => x.Quantity > 0))
            {
                throw new UserFriendlyException("Add at least one product to return.");
            }

            var sale = await _saleRepository.GetAllIncluding(x => x.Customer, x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == input.SaleId);

            if (sale == null)
            {
                throw new UserFriendlyException("Sale not found.");
            }

            if (!sale.Customer.AccountId.HasValue)
            {
                throw new UserFriendlyException("Customer has no linked account.");
            }

            if (input.ReturnDate == default)
            {
                input.ReturnDate = Abp.Timing.Clock.Now;
            }

            var returnedByLine = await GetReturnedQuantitiesBySaleLineAsync(sale.Id);
            var saleReturn = new SaleReturn
            {
                SaleId = sale.Id,
                ReturnDate = input.ReturnDate,
                Notes = input.Notes,
                Lines = new List<SaleReturnLine>()
            };

            decimal total = 0;
            foreach (var lineInput in input.Lines.Where(x => x.Quantity > 0))
            {
                var saleLine = sale.Lines.FirstOrDefault(x => x.Id == lineInput.SaleLineId);
                if (saleLine == null)
                {
                    throw new UserFriendlyException("Invalid sale line.");
                }

                returnedByLine.TryGetValue(saleLine.Id, out var alreadyReturned);
                var returnable = saleLine.Quantity - alreadyReturned;
                if (lineInput.Quantity > returnable)
                {
                    var product = await _productRepository.GetAsync(saleLine.ProductId);
                    throw new UserFriendlyException(
                        $"Return quantity for '{product.Name}' exceeds returnable quantity ({returnable}).");
                }

                var lineTotal = lineInput.Quantity * saleLine.UnitPrice;
                total += lineTotal;

                saleReturn.Lines.Add(new SaleReturnLine
                {
                    SaleLineId = saleLine.Id,
                    ProductId = saleLine.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitPrice = saleLine.UnitPrice,
                    LineTotal = lineTotal
                });

                var productEntity = await _productRepository.GetAsync(saleLine.ProductId);
                productEntity.StockQuantity += lineInput.Quantity;
                returnedByLine[saleLine.Id] = alreadyReturned + lineInput.Quantity;
            }

            if (!saleReturn.Lines.Any())
            {
                throw new UserFriendlyException("Add at least one product to return.");
            }

            saleReturn.TotalAmount = total;
            await _returnRepository.InsertAsync(saleReturn);
            await CurrentUnitOfWork.SaveChangesAsync();

            var saleAccount = await _systemAccountManager.GetSaleAccountAsync();
            var description = "Sale return for " + (sale.InvoiceNo.IsNullOrWhiteSpace() ? "#" + sale.Id : sale.InvoiceNo);

            // Reverse of sale: Debit Sale account, Credit Customer
            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = saleAccount.Id,
                TransactionDate = saleReturn.ReturnDate,
                VoucherType = VoucherTypes.SaleReturn,
                VoucherId = saleReturn.Id,
                Debit = saleReturn.TotalAmount,
                Credit = 0,
                Description = description
            });

            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = sale.Customer.AccountId.Value,
                TransactionDate = saleReturn.ReturnDate,
                VoucherType = VoucherTypes.SaleReturn,
                VoucherId = saleReturn.Id,
                Debit = 0,
                Credit = saleReturn.TotalAmount,
                Description = description
            });

            return await GetAsync(new EntityDto<int>(saleReturn.Id));
        }

        public async Task DeleteAsync(EntityDto<int> input)
        {
            var saleReturn = await _returnRepository.GetAllIncluding(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == input.Id);

            if (saleReturn == null)
            {
                throw new UserFriendlyException("Sale return not found.");
            }

            foreach (var line in saleReturn.Lines.ToList())
            {
                var product = await _productRepository.GetAsync(line.ProductId);
                product.StockQuantity -= line.Quantity;
                if (product.StockQuantity < 0)
                {
                    product.StockQuantity = 0;
                }

                await _returnLineRepository.DeleteAsync(line);
            }

            var ledgerEntries = await _ledgerRepository.GetAllListAsync(
                x => x.VoucherType == VoucherTypes.SaleReturn && x.VoucherId == saleReturn.Id);
            foreach (var entry in ledgerEntries)
            {
                await _ledgerRepository.DeleteAsync(entry);
            }

            await _returnRepository.DeleteAsync(saleReturn);
        }

        public async Task<PagedResultDto<SaleReturnDto>> GetAllAsync(PagedSaleReturnResultRequestDto input)
        {
            var query = _returnRepository.GetAllIncluding(x => x.Sale, x => x.Lines)
                .Include(x => x.Sale.Customer)
                .WhereIf(input.SaleId.HasValue, x => x.SaleId == input.SaleId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.Notes != null && x.Notes.Contains(input.Keyword))
                         || (x.Sale != null && x.Sale.InvoiceNo != null && x.Sale.InvoiceNo.Contains(input.Keyword))
                         || (x.Sale != null && x.Sale.Customer != null && x.Sale.Customer.Name.Contains(input.Keyword)));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.ReturnDate)
                .ThenByDescending(x => x.Id)
                .PageBy(input)
                .ToListAsync();

            foreach (var item in items)
            {
                foreach (var line in item.Lines)
                {
                    line.Product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
                }
            }

            return new PagedResultDto<SaleReturnDto>(totalCount, items.Select(MapToDto).ToList());
        }

        public async Task<SaleReturnDto> GetAsync(EntityDto<int> input)
        {
            var saleReturn = await _returnRepository.GetAllIncluding(x => x.Sale, x => x.Lines)
                .Include(x => x.Sale.Customer)
                .FirstOrDefaultAsync(x => x.Id == input.Id);

            if (saleReturn == null)
            {
                throw new UserFriendlyException("Sale return not found.");
            }

            foreach (var line in saleReturn.Lines)
            {
                line.Product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
            }

            return MapToDto(saleReturn);
        }

        private async Task<Dictionary<int, decimal>> GetReturnedQuantitiesBySaleLineAsync(int saleId)
        {
            var lines = await _returnLineRepository.GetAll()
                .Where(x => x.SaleReturn.SaleId == saleId)
                .GroupBy(x => x.SaleLineId)
                .Select(g => new { SaleLineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToListAsync();

            return lines.ToDictionary(x => x.SaleLineId, x => x.Quantity);
        }

        private static SaleReturnDto MapToDto(SaleReturn entity)
        {
            return new SaleReturnDto
            {
                Id = entity.Id,
                SaleId = entity.SaleId,
                SaleInvoiceNo = entity.Sale?.InvoiceNo,
                CustomerName = entity.Sale?.Customer?.Name,
                ReturnDate = entity.ReturnDate,
                TotalAmount = entity.TotalAmount,
                Notes = entity.Notes,
                Lines = entity.Lines?.Select(line => new SaleReturnLineDto
                {
                    Id = line.Id,
                    SaleReturnId = line.SaleReturnId,
                    SaleLineId = line.SaleLineId,
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    LineTotal = line.LineTotal
                }).ToList() ?? new List<SaleReturnLineDto>()
            };
        }
    }
}
