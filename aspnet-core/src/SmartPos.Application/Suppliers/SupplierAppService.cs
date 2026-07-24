using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using SmartPos.Authorization;
using SmartPos.Accounts;
using SmartPos.Suppliers.Dto;

namespace SmartPos.Suppliers
{
    [AbpAuthorize(PermissionNames.Pages_Suppliers)]
    public class SupplierAppService : AsyncCrudAppService<Supplier, SupplierDto, int, PagedSupplierResultRequestDto, CreateSupplierDto, SupplierDto>, ISupplierAppService
    {
        private readonly IRepository<BusinessAccount> _accountRepository;
        private readonly AccountBalanceManager _accountBalanceManager;

        public SupplierAppService(
            IRepository<Supplier> repository,
            IRepository<BusinessAccount> accountRepository,
            AccountBalanceManager accountBalanceManager)
            : base(repository)
        {
            _accountRepository = accountRepository;
            _accountBalanceManager = accountBalanceManager;
        }

        public override async Task<SupplierDto> CreateAsync(CreateSupplierDto input)
        {
            CheckCreatePermission();

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
            supplier.AccountId = account.Id;

            await Repository.InsertAsync(supplier);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(supplier.Id));
        }

        public override async Task<SupplierDto> UpdateAsync(SupplierDto input)
        {
            var dto = await base.UpdateAsync(input);
            if (dto.AccountId.HasValue)
            {
                var account = await _accountRepository.GetAsync(dto.AccountId.Value);
                account.Name = input.Name;
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            await FillBalance(dto);
            return dto;
        }

        public override async Task<SupplierDto> GetAsync(EntityDto<int> input)
        {
            await EnsureLinkedAccountAsync(input.Id);
            var dto = await base.GetAsync(input);
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
            var accountId = supplier.AccountId;

            await Repository.DeleteAsync(supplier);

            if (accountId.HasValue)
            {
                await _accountRepository.DeleteAsync(accountId.Value);
            }
        }

        protected override IQueryable<Supplier> CreateFilteredQuery(PagedSupplierResultRequestDto input)
        {
            return Repository.GetAllIncluding(x => x.Account)
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
            var missing = await Repository.GetAllListAsync(x => x.AccountId == null);
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
