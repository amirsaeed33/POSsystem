using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using SmartPos.Authorization;
using SmartPos.Accounts.Dto;

namespace SmartPos.Accounts
{
    [AbpAuthorize(PermissionNames.Pages_Accounts)]
    public class BusinessAccountAppService : AsyncCrudAppService<BusinessAccount, BusinessAccountDto, int, PagedBusinessAccountResultRequestDto, CreateBusinessAccountDto, BusinessAccountDto>, IBusinessAccountAppService
    {
        private readonly AccountBalanceManager _accountBalanceManager;
        private readonly SystemAccountManager _systemAccountManager;

        public BusinessAccountAppService(
            IRepository<BusinessAccount> repository,
            AccountBalanceManager accountBalanceManager,
            SystemAccountManager systemAccountManager)
            : base(repository)
        {
            _accountBalanceManager = accountBalanceManager;
            _systemAccountManager = systemAccountManager;
        }

        public override async Task<BusinessAccountDto> CreateAsync(CreateBusinessAccountDto input)
        {
            CheckCreatePermission();

            var account = ObjectMapper.Map<BusinessAccount>(input);
            await Repository.InsertAsync(account);
            await CurrentUnitOfWork.SaveChangesAsync();

            await _accountBalanceManager.InsertOpeningBalanceAsync(account.Id, account.OpeningBalance);

            return await GetAsync(new EntityDto<int>(account.Id));
        }

        public override async Task<BusinessAccountDto> GetAsync(EntityDto<int> input)
        {
            var account = await Repository.GetAsync(input.Id);
            await _accountBalanceManager.EnsureOpeningBalancePostedAsync(account);
            await CurrentUnitOfWork.SaveChangesAsync();

            var dto = await base.GetAsync(input);
            dto.Balance = await _accountBalanceManager.GetBalanceAsync(dto.Id);
            return dto;
        }

        public override async Task<PagedResultDto<BusinessAccountDto>> GetAllAsync(PagedBusinessAccountResultRequestDto input)
        {
            await _systemAccountManager.EnsureSystemAccountsAsync();

            var accounts = await Repository.GetAllListAsync();
            foreach (var account in accounts)
            {
                await _accountBalanceManager.EnsureOpeningBalancePostedAsync(account);
            }
            await CurrentUnitOfWork.SaveChangesAsync();

            var result = await base.GetAllAsync(input);
            foreach (var item in result.Items)
            {
                item.Balance = await _accountBalanceManager.GetBalanceAsync(item.Id);
            }
            return result;
        }

        protected override IQueryable<BusinessAccount> CreateFilteredQuery(PagedBusinessAccountResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Code != null && x.Code.Contains(input.Keyword))
                         || (x.AccountType != null && x.AccountType.Contains(input.Keyword))
                         || (x.Description != null && x.Description.Contains(input.Keyword)))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive.Value);
        }
    }
}
