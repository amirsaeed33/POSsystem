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
using SmartPos.Purchases.Dto;

namespace SmartPos.Purchases
{
    [AbpAuthorize(PermissionNames.Pages_Purchases)]
    public class PurchaseReturnAppService : ApplicationService, IPurchaseReturnAppService
    {
        private readonly IRepository<Purchase> _purchaseRepository;
        private readonly IRepository<PurchaseReturn> _returnRepository;
        private readonly IRepository<PurchaseReturnLine> _returnLineRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<LedgerEntry> _ledgerRepository;
        private readonly SystemAccountManager _systemAccountManager;

        public PurchaseReturnAppService(
            IRepository<Purchase> purchaseRepository,
            IRepository<PurchaseReturn> returnRepository,
            IRepository<PurchaseReturnLine> returnLineRepository,
            IRepository<Product> productRepository,
            IRepository<LedgerEntry> ledgerRepository,
            SystemAccountManager systemAccountManager)
        {
            _purchaseRepository = purchaseRepository;
            _returnRepository = returnRepository;
            _returnLineRepository = returnLineRepository;
            _productRepository = productRepository;
            _ledgerRepository = ledgerRepository;
            _systemAccountManager = systemAccountManager;
        }

        public async Task<PurchaseReturnableDto> GetReturnablePurchaseAsync(EntityDto<int> input)
        {
            var purchase = await _purchaseRepository.GetAllIncluding(x => x.Supplier, x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == input.Id);

            if (purchase == null)
            {
                throw new UserFriendlyException("Purchase not found.");
            }

            var returnedByLine = await GetReturnedQuantitiesByPurchaseLineAsync(purchase.Id);
            var lines = new List<PurchaseReturnableLineDto>();

            foreach (var line in purchase.Lines.OrderBy(x => x.Id))
            {
                line.Product ??= await _productRepository.FirstOrDefaultAsync(line.ProductId);
                returnedByLine.TryGetValue(line.Id, out var returnedQty);
                var returnable = line.Quantity - returnedQty;
                if (returnable <= 0)
                {
                    continue;
                }

                lines.Add(new PurchaseReturnableLineDto
                {
                    Id = line.Id,
                    PurchaseLineId = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name,
                    PurchasedQuantity = line.Quantity,
                    ReturnedQuantity = returnedQty,
                    ReturnableQuantity = returnable,
                    UnitCost = line.UnitCost
                });
            }

            return new PurchaseReturnableDto
            {
                Id = purchase.Id,
                SupplierId = purchase.SupplierId,
                SupplierName = purchase.Supplier?.Name,
                InvoiceNo = purchase.InvoiceNo,
                PurchaseDate = purchase.PurchaseDate,
                Lines = lines
            };
        }

        public async Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnDto input)
        {
            if (input.Lines == null || !input.Lines.Any(x => x.Quantity > 0))
            {
                throw new UserFriendlyException("Add at least one product to return.");
            }

            var purchase = await _purchaseRepository.GetAllIncluding(x => x.Supplier, x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == input.PurchaseId);

            if (purchase == null)
            {
                throw new UserFriendlyException("Purchase not found.");
            }

            if (!purchase.Supplier.AccountId.HasValue)
            {
                throw new UserFriendlyException("Supplier has no linked account.");
            }

            if (input.ReturnDate == default)
            {
                input.ReturnDate = Abp.Timing.Clock.Now;
            }

            var returnedByLine = await GetReturnedQuantitiesByPurchaseLineAsync(purchase.Id);
            var purchaseReturn = new PurchaseReturn
            {
                PurchaseId = purchase.Id,
                ReturnDate = input.ReturnDate,
                Notes = input.Notes,
                Lines = new List<PurchaseReturnLine>()
            };

            decimal total = 0;
            foreach (var lineInput in input.Lines.Where(x => x.Quantity > 0))
            {
                var purchaseLine = purchase.Lines.FirstOrDefault(x => x.Id == lineInput.PurchaseLineId);
                if (purchaseLine == null)
                {
                    throw new UserFriendlyException("Invalid purchase line.");
                }

                returnedByLine.TryGetValue(purchaseLine.Id, out var alreadyReturned);
                var returnable = purchaseLine.Quantity - alreadyReturned;
                if (lineInput.Quantity > returnable)
                {
                    var product = await _productRepository.GetAsync(purchaseLine.ProductId);
                    throw new UserFriendlyException(
                        $"Return quantity for '{product.Name}' exceeds returnable quantity ({returnable}).");
                }

                var productEntity = await _productRepository.GetAsync(purchaseLine.ProductId);
                if (productEntity.StockQuantity < lineInput.Quantity)
                {
                    throw new UserFriendlyException(
                        $"Insufficient stock to return '{productEntity.Name}'. Available: {productEntity.StockQuantity}.");
                }

                var lineTotal = lineInput.Quantity * purchaseLine.UnitCost;
                total += lineTotal;

                purchaseReturn.Lines.Add(new PurchaseReturnLine
                {
                    PurchaseLineId = purchaseLine.Id,
                    ProductId = purchaseLine.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitCost = purchaseLine.UnitCost,
                    LineTotal = lineTotal
                });

                productEntity.StockQuantity -= lineInput.Quantity;
                returnedByLine[purchaseLine.Id] = alreadyReturned + lineInput.Quantity;
            }

            if (!purchaseReturn.Lines.Any())
            {
                throw new UserFriendlyException("Add at least one product to return.");
            }

            purchaseReturn.TotalAmount = total;
            await _returnRepository.InsertAsync(purchaseReturn);
            await CurrentUnitOfWork.SaveChangesAsync();

            var purchaseAccount = await _systemAccountManager.GetPurchaseAccountAsync();
            var description = "Purchase return for " + (purchase.InvoiceNo.IsNullOrWhiteSpace() ? "#" + purchase.Id : purchase.InvoiceNo);

            // Reverse of purchase: Debit Supplier, Credit Purchase account
            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = purchase.Supplier.AccountId.Value,
                TransactionDate = purchaseReturn.ReturnDate,
                VoucherType = VoucherTypes.PurchaseReturn,
                VoucherId = purchaseReturn.Id,
                Debit = purchaseReturn.TotalAmount,
                Credit = 0,
                Description = description
            });

            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = purchaseAccount.Id,
                TransactionDate = purchaseReturn.ReturnDate,
                VoucherType = VoucherTypes.PurchaseReturn,
                VoucherId = purchaseReturn.Id,
                Debit = 0,
                Credit = purchaseReturn.TotalAmount,
                Description = description
            });

            return await GetAsync(new EntityDto<int>(purchaseReturn.Id));
        }

        public async Task DeleteAsync(EntityDto<int> input)
        {
            var purchaseReturn = await _returnRepository.GetAllIncluding(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == input.Id);

            if (purchaseReturn == null)
            {
                throw new UserFriendlyException("Purchase return not found.");
            }

            foreach (var line in purchaseReturn.Lines.ToList())
            {
                var product = await _productRepository.GetAsync(line.ProductId);
                product.StockQuantity += line.Quantity;
                await _returnLineRepository.DeleteAsync(line);
            }

            var ledgerEntries = await _ledgerRepository.GetAllListAsync(
                x => x.VoucherType == VoucherTypes.PurchaseReturn && x.VoucherId == purchaseReturn.Id);
            foreach (var entry in ledgerEntries)
            {
                await _ledgerRepository.DeleteAsync(entry);
            }

            await _returnRepository.DeleteAsync(purchaseReturn);
        }

        public async Task<PagedResultDto<PurchaseReturnDto>> GetAllAsync(PagedPurchaseReturnResultRequestDto input)
        {
            var query = _returnRepository.GetAllIncluding(x => x.Purchase, x => x.Lines)
                .Include(x => x.Purchase.Supplier)
                .WhereIf(input.PurchaseId.HasValue, x => x.PurchaseId == input.PurchaseId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.Notes != null && x.Notes.Contains(input.Keyword))
                         || (x.Purchase != null && x.Purchase.InvoiceNo != null && x.Purchase.InvoiceNo.Contains(input.Keyword))
                         || (x.Purchase != null && x.Purchase.Supplier != null && x.Purchase.Supplier.Name.Contains(input.Keyword)));

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

            return new PagedResultDto<PurchaseReturnDto>(totalCount, items.Select(MapToDto).ToList());
        }

        public async Task<PurchaseReturnDto> GetAsync(EntityDto<int> input)
        {
            var purchaseReturn = await _returnRepository.GetAllIncluding(x => x.Purchase, x => x.Lines)
                .Include(x => x.Purchase.Supplier)
                .FirstOrDefaultAsync(x => x.Id == input.Id);

            if (purchaseReturn == null)
            {
                throw new UserFriendlyException("Purchase return not found.");
            }

            foreach (var line in purchaseReturn.Lines)
            {
                line.Product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
            }

            return MapToDto(purchaseReturn);
        }

        private async Task<Dictionary<int, decimal>> GetReturnedQuantitiesByPurchaseLineAsync(int purchaseId)
        {
            var lines = await _returnLineRepository.GetAll()
                .Where(x => x.PurchaseReturn.PurchaseId == purchaseId)
                .GroupBy(x => x.PurchaseLineId)
                .Select(g => new { PurchaseLineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToListAsync();

            return lines.ToDictionary(x => x.PurchaseLineId, x => x.Quantity);
        }

        private static PurchaseReturnDto MapToDto(PurchaseReturn entity)
        {
            return new PurchaseReturnDto
            {
                Id = entity.Id,
                PurchaseId = entity.PurchaseId,
                PurchaseInvoiceNo = entity.Purchase?.InvoiceNo,
                SupplierName = entity.Purchase?.Supplier?.Name,
                ReturnDate = entity.ReturnDate,
                TotalAmount = entity.TotalAmount,
                Notes = entity.Notes,
                Lines = entity.Lines?.Select(line => new PurchaseReturnLineDto
                {
                    Id = line.Id,
                    PurchaseReturnId = line.PurchaseReturnId,
                    PurchaseLineId = line.PurchaseLineId,
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name,
                    Quantity = line.Quantity,
                    UnitCost = line.UnitCost,
                    LineTotal = line.LineTotal
                }).ToList() ?? new List<PurchaseReturnLineDto>()
            };
        }
    }
}
