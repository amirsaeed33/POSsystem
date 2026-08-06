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
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Inventory;
using SmartPos.Products;
using SmartPos.Purchases.Dto;
using SmartPos.Suppliers;

namespace SmartPos.Purchases
{
    [AbpAuthorize(PermissionNames.Pages_Purchases)]
    public class PurchaseAppService : AsyncCrudAppService<Purchase, PurchaseDto, int, PagedPurchaseResultRequestDto, CreatePurchaseDto, PurchaseDto>, IPurchaseAppService
    {
        private readonly IRepository<PurchaseLine> _lineRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly IRepository<LedgerEntry> _ledgerRepository;
        private readonly IRepository<PurchaseReturn> _purchaseReturnRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly IBranchStockManager _branchStockManager;
        private readonly SystemAccountManager _systemAccountManager;

        public PurchaseAppService(
            IRepository<Purchase> repository,
            IRepository<PurchaseLine> lineRepository,
            IRepository<Product> productRepository,
            IRepository<Supplier> supplierRepository,
            IRepository<LedgerEntry> ledgerRepository,
            IRepository<PurchaseReturn> purchaseReturnRepository,
            IRepository<User, long> userRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            IBranchStockManager branchStockManager,
            SystemAccountManager systemAccountManager)
            : base(repository)
        {
            _lineRepository = lineRepository;
            _productRepository = productRepository;
            _supplierRepository = supplierRepository;
            _ledgerRepository = ledgerRepository;
            _purchaseReturnRepository = purchaseReturnRepository;
            _userRepository = userRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _branchStockManager = branchStockManager;
            _systemAccountManager = systemAccountManager;
        }

        public override async Task<PurchaseDto> CreateAsync(CreatePurchaseDto input)
        {
            CheckCreatePermission();

            if (input.Lines == null || !input.Lines.Any())
            {
                throw new UserFriendlyException("Add at least one product line.");
            }

            var supplier = await _supplierRepository.GetAsync(input.SupplierId);
            if (!supplier.AccountId.HasValue)
            {
                throw new UserFriendlyException("Supplier has no linked account. Open Suppliers once to create it.");
            }

            if (input.PurchaseDate == default)
            {
                input.PurchaseDate = Abp.Timing.Clock.Now;
            }

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();

            var purchase = new Purchase
            {
                TenantId = AbpSession.TenantId,
                BranchId = branchId,
                SupplierId = input.SupplierId,
                PurchaseDate = input.PurchaseDate,
                Notes = input.Notes,
                Lines = new List<PurchaseLine>()
            };

            decimal total = 0;
            foreach (var lineInput in input.Lines)
            {
                if (lineInput.Quantity <= 0)
                {
                    throw new UserFriendlyException("Quantity must be greater than zero.");
                }

                var product = await _productRepository.GetAsync(lineInput.ProductId);
                await _branchStockManager.EnsureCanUseProductAtBranchAsync(branchId, lineInput.ProductId);
                var lineTotal = lineInput.Quantity * lineInput.UnitCost;
                total += lineTotal;

                purchase.Lines.Add(new PurchaseLine
                {
                    ProductId = lineInput.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitCost = lineInput.UnitCost,
                    LineTotal = lineTotal
                });

                var branchInfo = await _branchStockManager.GetBranchProductInfoAsync(
                    branchId,
                    new[] { lineInput.ProductId });
                var current = branchInfo[lineInput.ProductId];
                var newBranchCost = ProductPricing.CalculateAverageCost(
                    current.Quantity,
                    current.CostPrice,
                    lineInput.Quantity,
                    lineInput.UnitCost);

                ProductPricing.ApplyPurchaseCost(product, lineInput.Quantity, lineInput.UnitCost);
                await _branchStockManager.IncreaseAsync(branchId, lineInput.ProductId, lineInput.Quantity);
                await _branchStockManager.SetPricesAsync(
                    branchId,
                    lineInput.ProductId,
                    current.Price,
                    current.WholesalePrice,
                    newBranchCost);
            }

            purchase.TotalAmount = total;

            await Repository.InsertAsync(purchase);
            await CurrentUnitOfWork.SaveChangesAsync();

            purchase.InvoiceNo = "PUR-" + purchase.Id.ToString("D6");

            var purchaseAccount = await _systemAccountManager.GetPurchaseAccountAsync();
            var description = "Purchase " + purchase.InvoiceNo;

            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = purchaseAccount.Id,
                TransactionDate = purchase.PurchaseDate,
                VoucherType = VoucherTypes.Invoice,
                VoucherId = purchase.Id,
                Debit = purchase.TotalAmount,
                Credit = 0,
                Description = description
            });

            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = supplier.AccountId.Value,
                TransactionDate = purchase.PurchaseDate,
                VoucherType = VoucherTypes.Invoice,
                VoucherId = purchase.Id,
                Debit = 0,
                Credit = purchase.TotalAmount,
                Description = description
            });

            return await GetAsync(new EntityDto<int>(purchase.Id));
        }

        public override async Task<PurchaseDto> UpdateAsync(PurchaseDto input)
        {
            throw new UserFriendlyException("Purchases cannot be edited. Delete and create a new purchase instead.");
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var purchase = await GetEntityByIdAsync(input.Id);
            if (purchase == null)
            {
                throw new UserFriendlyException("Purchase not found.");
            }

            var hasReturns = await _purchaseReturnRepository.CountAsync(x => x.PurchaseId == purchase.Id) > 0;
            if (hasReturns)
            {
                throw new UserFriendlyException("Cannot delete this purchase because it has product returns. Delete the returns first.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(purchase.BranchId);

            foreach (var line in purchase.Lines.ToList())
            {
                var product = await _productRepository.GetAsync(line.ProductId);
                await _branchStockManager.DecreaseAsync(purchase.BranchId, line.ProductId, line.Quantity, product.Name);
                await _lineRepository.DeleteAsync(line);
            }

            var ledgerEntries = await _ledgerRepository.GetAllListAsync(
                x => x.VoucherType == VoucherTypes.Invoice && x.VoucherId == purchase.Id);
            foreach (var entry in ledgerEntries)
            {
                await _ledgerRepository.DeleteAsync(entry);
            }

            await Repository.DeleteAsync(purchase);
        }

        protected override IQueryable<Purchase> CreateFilteredQuery(PagedPurchaseResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            return Repository.GetAllIncluding(x => x.Supplier, x => x.Lines)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(input.SupplierId.HasValue, x => x.SupplierId == input.SupplierId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.InvoiceNo != null && x.InvoiceNo.Contains(input.Keyword))
                         || (x.Notes != null && x.Notes.Contains(input.Keyword))
                         || (x.Supplier != null && x.Supplier.Name.Contains(input.Keyword)));
        }

        protected override async Task<Purchase> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Supplier, x => x.Lines)
                    .Where(x => x.Id == id));
        }

        protected override PurchaseDto MapToEntityDto(Purchase entity)
        {
            var dto = new PurchaseDto
            {
                Id = entity.Id,
                SupplierId = entity.SupplierId,
                SupplierName = entity.Supplier?.Name,
                PurchaseDate = entity.PurchaseDate,
                InvoiceNo = entity.InvoiceNo,
                TotalAmount = entity.TotalAmount,
                Notes = entity.Notes,
                Lines = new List<PurchaseLineDto>()
            };

            if (entity.Lines != null)
            {
                foreach (var line in entity.Lines)
                {
                    dto.Lines.Add(new PurchaseLineDto
                    {
                        Id = line.Id,
                        PurchaseId = line.PurchaseId,
                        ProductId = line.ProductId,
                        ProductName = line.Product?.Name,
                        Quantity = line.Quantity,
                        UnitCost = line.UnitCost,
                        LineTotal = line.LineTotal
                    });
                }
            }

            return dto;
        }

        public override async Task<PurchaseDto> GetAsync(EntityDto<int> input)
        {
            var entity = await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Supplier, x => x.Lines)
                    .Where(x => x.Id == input.Id));

            if (entity == null)
            {
                throw new UserFriendlyException("Purchase not found.");
            }

            foreach (var line in entity.Lines)
            {
                line.Product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
            }

            return MapToEntityDto(entity);
        }
    }
}
