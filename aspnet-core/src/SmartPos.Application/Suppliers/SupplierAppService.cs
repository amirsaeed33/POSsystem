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
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Accounts;
using SmartPos.Branches;
using SmartPos.Products;
using SmartPos.Suppliers.Dto;

namespace SmartPos.Suppliers
{
    [AbpAuthorize(PermissionNames.Pages_Suppliers)]
    public class SupplierAppService : AsyncCrudAppService<Supplier, SupplierDto, int, PagedSupplierResultRequestDto, CreateSupplierDto, SupplierDto>, ISupplierAppService
    {
        private readonly IRepository<BusinessAccount> _accountRepository;
        private readonly IRepository<SupplierProduct> _supplierProductRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly AccountBalanceManager _accountBalanceManager;

        public SupplierAppService(
            IRepository<Supplier> repository,
            IRepository<BusinessAccount> accountRepository,
            IRepository<SupplierProduct> supplierProductRepository,
            IRepository<Product> productRepository,
            IRepository<Branch> branchRepository,
            IRepository<User, long> userRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            AccountBalanceManager accountBalanceManager)
            : base(repository)
        {
            _accountRepository = accountRepository;
            _supplierProductRepository = supplierProductRepository;
            _productRepository = productRepository;
            _branchRepository = branchRepository;
            _userRepository = userRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _accountBalanceManager = accountBalanceManager;
            CreatePermissionName = PermissionNames.Pages_Suppliers_Create;
            UpdatePermissionName = PermissionNames.Pages_Suppliers_Edit;
            DeletePermissionName = PermissionNames.Pages_Suppliers_Delete;
            GetPermissionName = PermissionNames.Pages_Suppliers;
            GetAllPermissionName = PermissionNames.Pages_Suppliers;
        }

        public override async Task<SupplierDto> CreateAsync(CreateSupplierDto input)
        {
            CheckCreatePermission();

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();

            var account = new BusinessAccount
            {
                Name = input.Name,
                Code = "SUP-" + Abp.Timing.Clock.Now.ToString("yyyyMMddHHmmss"),
                AccountType = AccountTypes.Supplier,
                OpeningBalance = 0,
                Description = "Supplier account: " + input.Name,
                IsActive = true
            };

            await _accountRepository.InsertAsync(account);
            await CurrentUnitOfWork.SaveChangesAsync();

            var supplier = ObjectMapper.Map<Supplier>(input);
            supplier.BranchId = branchId;
            supplier.AccountId = account.Id;

            await Repository.InsertAsync(supplier);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(supplier.Id));
        }

        public override async Task<SupplierDto> UpdateAsync(SupplierDto input)
        {
            var supplier = await Repository.GetAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(supplier.BranchId);

            var branchId = supplier.BranchId;
            MapToEntity(input, supplier);
            supplier.BranchId = branchId;

            await CurrentUnitOfWork.SaveChangesAsync();

            if (supplier.AccountId.HasValue)
            {
                var account = await _accountRepository.GetAsync(supplier.AccountId.Value);
                account.Name = input.Name;
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return await GetAsync(new EntityDto<int>(supplier.Id));
        }

        public override async Task<SupplierDto> GetAsync(EntityDto<int> input)
        {
            await EnsureLinkedAccountAsync(input.Id);
            var dto = await base.GetAsync(input);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(dto.BranchId);
            await FillBalance(dto);
            return dto;
        }

        public override async Task<PagedResultDto<SupplierDto>> GetAllAsync(PagedSupplierResultRequestDto input)
        {
            await BackfillMissingAccountsAsync();
            var result = await base.GetAllAsync(input);
            foreach (var item in result.Items)
            {
                await FillBalance(item);
            }
            return result;
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var supplier = await Repository.GetAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(supplier.BranchId);
            var accountId = supplier.AccountId;

            var links = await _supplierProductRepository.GetAllListAsync(x => x.SupplierId == supplier.Id);
            foreach (var link in links)
            {
                await _supplierProductRepository.DeleteAsync(link);
            }

            await Repository.DeleteAsync(supplier);

            if (accountId.HasValue)
            {
                await _accountRepository.DeleteAsync(accountId.Value);
            }
        }

        public async Task<List<int>> GetAssignedProductIdsAsync(EntityDto input)
        {
            var supplier = await Repository.GetAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(supplier.BranchId);

            return await _supplierProductRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.SupplierId == supplier.Id)
                .Select(x => x.ProductId)
                .ToListAsync();
        }

        [AbpAuthorize(PermissionNames.Pages_Suppliers_Edit)]
        public async Task SetAssignedProductsAsync(SetSupplierProductsDto input)
        {
            if (input == null || input.SupplierId <= 0)
            {
                throw new UserFriendlyException("Supplier is required.");
            }

            var supplier = await Repository.GetAsync(input.SupplierId);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(supplier.BranchId);

            var productIds = (input.ProductIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (productIds.Count > 0)
            {
                var products = await _productRepository.GetAll()
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                if (products.Count != productIds.Count)
                {
                    throw new UserFriendlyException("One or more selected products are invalid.");
                }

                var branch = await _branchRepository.FirstOrDefaultAsync(supplier.BranchId);
                if (branch != null && branch.BusinessType == BusinessType.Restaurant)
                {
                    var invalid = products.FirstOrDefault(p =>
                        !PermanentGrocery.IsInventoryProduct(p.Barcode, p.Name, p.Category?.Name));
                    if (invalid != null)
                    {
                        throw new UserFriendlyException(
                            $"Only Permanent Grocery products can be assigned for restaurant suppliers. Invalid: '{invalid.Name}'.");
                    }
                }
            }

            var existing = await _supplierProductRepository.GetAllListAsync(x => x.SupplierId == supplier.Id);
            var existingIds = existing.Select(x => x.ProductId).ToHashSet();
            var desiredIds = productIds.ToHashSet();

            foreach (var link in existing.Where(x => !desiredIds.Contains(x.ProductId)))
            {
                await _supplierProductRepository.DeleteAsync(link);
            }

            foreach (var productId in productIds.Where(id => !existingIds.Contains(id)))
            {
                await _supplierProductRepository.InsertAsync(new SupplierProduct
                {
                    TenantId = AbpSession.TenantId,
                    SupplierId = supplier.Id,
                    ProductId = productId
                });
            }
        }

        protected override IQueryable<Supplier> CreateFilteredQuery(PagedSupplierResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            return Repository.GetAllIncluding(x => x.Account)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Phone != null && x.Phone.Contains(input.Keyword))
                         || (x.Email != null && x.Email.Contains(input.Keyword))
                         || (x.Address != null && x.Address.Contains(input.Keyword))
                         || (x.Description != null && x.Description.Contains(input.Keyword)));
        }

        protected override async Task<Supplier> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Account).Where(x => x.Id == id));
        }

        private async Task FillBalance(SupplierDto dto)
        {
            if (dto.AccountId.HasValue)
            {
                dto.Balance = await _accountBalanceManager.GetBalanceAsync(dto.AccountId.Value);
            }
        }

        private async Task BackfillMissingAccountsAsync()
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);
            var missing = await Repository.GetAllListAsync(x =>
                x.AccountId == null && (!branchId.HasValue || x.BranchId == branchId.Value));
            foreach (var supplier in missing)
            {
                await CreateLinkedAccountAsync(supplier);
            }
        }

        private async Task EnsureLinkedAccountAsync(int supplierId)
        {
            var supplier = await Repository.GetAsync(supplierId);
            if (!supplier.AccountId.HasValue)
            {
                await CreateLinkedAccountAsync(supplier);
            }
        }

        private async Task CreateLinkedAccountAsync(Supplier supplier)
        {
            var account = new BusinessAccount
            {
                Name = supplier.Name,
                Code = "SUP-" + supplier.Id,
                AccountType = AccountTypes.Supplier,
                OpeningBalance = 0,
                Description = "Supplier account: " + supplier.Name,
                IsActive = true
            };

            await _accountRepository.InsertAsync(account);
            await CurrentUnitOfWork.SaveChangesAsync();

            supplier.AccountId = account.Id;
            await CurrentUnitOfWork.SaveChangesAsync();
        }
    }
}
