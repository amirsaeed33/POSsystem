using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches.Dto;
using SmartPos.Inventory;
using SmartPos.Products;

namespace SmartPos.Branches
{
    [AbpAuthorize]
    public class BranchAppService : AsyncCrudAppService<Branch, BranchDto, int, PagedBranchResultRequestDto, CreateBranchDto, BranchDto>, IBranchAppService
    {
        public UserManager UserManager { get; set; }

        private readonly IRepository<Product> _productRepository;
        private readonly IBranchStockManager _branchStockManager;

        public BranchAppService(
            IRepository<Branch> repository,
            IRepository<Product> productRepository,
            IBranchStockManager branchStockManager)
            : base(repository)
        {
            _productRepository = productRepository;
            _branchStockManager = branchStockManager;
            CreatePermissionName = PermissionNames.Pages_Branches;
            UpdatePermissionName = PermissionNames.Pages_Branches;
            DeletePermissionName = PermissionNames.Pages_Branches;
            GetPermissionName = PermissionNames.Pages_Branches;
            GetAllPermissionName = PermissionNames.Pages_Branches;
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<BranchDto> CreateAsync(CreateBranchDto input)
        {
            CheckCreatePermission();

            var branch = ObjectMapper.Map<Branch>(input);
            branch.TenantId = AbpSession.TenantId;
            branch.ImagePath = BranchImageStore.SaveBase64Image(input.ImageBase64);

            await Repository.InsertAsync(branch);
            await CurrentUnitOfWork.SaveChangesAsync();

            await SeedSharedProductsAsync(branch.Id);

            return await GetAsync(new EntityDto<int>(branch.Id));
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<BranchDto> UpdateAsync(BranchDto input)
        {
            CheckUpdatePermission();

            var branch = await GetEntityByIdAsync(input.Id);

            branch.Name = input.Name;
            branch.Code = input.Code;
            branch.IsActive = input.IsActive;
            branch.IsDefault = input.IsDefault;
            branch.InvoiceAddress = input.InvoiceAddress;
            branch.InvoiceContactEmail = input.InvoiceContactEmail;
            branch.InvoiceContactPhone = input.InvoiceContactPhone;
            branch.TaxNumber = input.TaxNumber;
            branch.Website = input.Website;
            branch.InvoiceFooter = input.InvoiceFooter;

            if (BranchImageStore.IsNewImagePayload(input.ImageBase64))
            {
                BranchImageStore.DeleteIfExists(branch.ImagePath);
                branch.ImagePath = BranchImageStore.SaveBase64Image(input.ImageBase64);
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(branch.Id));
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var branch = await Repository.GetAsync(input.Id);
            BranchImageStore.DeleteIfExists(branch.ImagePath);
            await Repository.DeleteAsync(branch);
        }

        /// <summary>
        /// Allowed branches for the current user: assigned branch only, or all active if Pages.Branches.
        /// </summary>
        public async Task<ListResultDto<BranchDto>> GetLookupAsync()
        {
            IQueryable<Branch> query = Repository.GetAll().Where(x => x.IsActive);

            if (!await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches))
            {
                if (!AbpSession.UserId.HasValue)
                {
                    return new ListResultDto<BranchDto>(new List<BranchDto>());
                }

                var currentUser = await UserManager.GetUserByIdAsync(AbpSession.UserId.Value);
                if (!currentUser.BranchId.HasValue)
                {
                    return new ListResultDto<BranchDto>(new List<BranchDto>());
                }

                query = query.Where(x => x.Id == currentUser.BranchId.Value);
            }

            var branches = await query.OrderBy(x => x.Name).ToListAsync();
            return new ListResultDto<BranchDto>(ObjectMapper.Map<List<BranchDto>>(branches));
        }

        public async Task<BranchDto> GetInvoiceInfoAsync()
        {
            Branch branch = null;

            if (AbpSession.UserId.HasValue)
            {
                var currentUser = await UserManager.FindByIdAsync(AbpSession.UserId.Value.ToString());
                if (currentUser?.BranchId != null)
                {
                    branch = await Repository.FirstOrDefaultAsync(currentUser.BranchId.Value);
                }
            }

            if (branch == null)
            {
                branch = await Repository.GetAll()
                    .OrderByDescending(x => x.IsDefault)
                    .ThenBy(x => x.Id)
                    .FirstOrDefaultAsync();
            }

            return branch == null ? null : ObjectMapper.Map<BranchDto>(branch);
        }

        protected override IQueryable<Branch> CreateFilteredQuery(PagedBranchResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || x.Code.Contains(input.Keyword)
                         || (x.InvoiceAddress != null && x.InvoiceAddress.Contains(input.Keyword))
                         || (x.InvoiceContactEmail != null && x.InvoiceContactEmail.Contains(input.Keyword))
                         || (x.InvoiceContactPhone != null && x.InvoiceContactPhone.Contains(input.Keyword))
                         || (x.TaxNumber != null && x.TaxNumber.Contains(input.Keyword)));
        }

        private async Task SeedSharedProductsAsync(int branchId)
        {
            var sharedProducts = await _productRepository.GetAll()
                .Where(x => x.IsShared)
                .Select(x => new
                {
                    x.Id,
                    x.Price,
                    x.WholesalePrice,
                    x.CostPrice
                })
                .ToListAsync();

            foreach (var product in sharedProducts)
            {
                await _branchStockManager.UpsertStockAndPricesAsync(
                    branchId,
                    product.Id,
                    0,
                    product.Price,
                    product.WholesalePrice,
                    product.CostPrice);
            }
        }
    }
}
