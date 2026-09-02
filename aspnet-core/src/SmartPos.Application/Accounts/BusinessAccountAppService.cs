using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Accounts.Dto;
using SmartPos.Lookups;

namespace SmartPos.Accounts
{
    [AbpAuthorize(PermissionNames.Pages_Accounts)]
    public class BusinessAccountAppService : AsyncCrudAppService<BusinessAccount, BusinessAccountDto, int, PagedBusinessAccountResultRequestDto, CreateBusinessAccountDto, BusinessAccountDto>, IBusinessAccountAppService
    {
        private readonly AccountBalanceManager _accountBalanceManager;
        private readonly SystemAccountManager _systemAccountManager;
        private readonly IRepository<LookUp> _lookUpRepository;

        public BusinessAccountAppService(
            IRepository<BusinessAccount> repository,
            AccountBalanceManager accountBalanceManager,
            SystemAccountManager systemAccountManager,
            IRepository<LookUp> lookUpRepository)
            : base(repository)
        {
            _accountBalanceManager = accountBalanceManager;
            _systemAccountManager = systemAccountManager;
            _lookUpRepository = lookUpRepository;
            CreatePermissionName = PermissionNames.Pages_Accounts_Create;
            UpdatePermissionName = PermissionNames.Pages_Accounts_Edit;
            DeletePermissionName = PermissionNames.Pages_Accounts_Delete;
            GetPermissionName = PermissionNames.Pages_Accounts;
            GetAllPermissionName = PermissionNames.Pages_Accounts;
        }

        public override async Task<BusinessAccountDto> CreateAsync(CreateBusinessAccountDto input)
        {
            CheckCreatePermission();

            var account = ObjectMapper.Map<BusinessAccount>(input);
            if (account.AccountTypeId.HasValue)
            {
                var lookup = await _lookUpRepository.FirstOrDefaultAsync(account.AccountTypeId.Value);
                if (lookup != null)
                {
                    account.AccountType = lookup.DisplayName ?? lookup.Name;
                }
            }
            await Repository.InsertAsync(account);
            await CurrentUnitOfWork.SaveChangesAsync();

            await _accountBalanceManager.InsertOpeningBalanceAsync(account.Id, account.OpeningBalance);

            return await GetAsync(new EntityDto<int>(account.Id));
        }

        public override async Task<BusinessAccountDto> UpdateAsync(BusinessAccountDto input)
        {
            CheckUpdatePermission();

            var account = await Repository.GetAsync(input.Id);
            ObjectMapper.Map(input, account);

            if (account.AccountTypeId.HasValue)
            {
                var lookup = await _lookUpRepository.FirstOrDefaultAsync(account.AccountTypeId.Value);
                if (lookup != null)
                {
                    account.AccountType = lookup.DisplayName ?? lookup.Name;
                }
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            return await GetAsync(new EntityDto<int>(account.Id));
        }

        public override async Task<BusinessAccountDto> GetAsync(EntityDto<int> input)
        {
            var account = await Repository.GetAll()
                .Include(x => x.AccountTypeLookup)
                .FirstOrDefaultAsync(x => x.Id == input.Id);
            if (account == null)
            {
                throw new EntityNotFoundException(typeof(BusinessAccount), input.Id);
            }
            await _accountBalanceManager.EnsureOpeningBalancePostedAsync(account);
            await CurrentUnitOfWork.SaveChangesAsync();

            var dto = ObjectMapper.Map<BusinessAccountDto>(account);
            dto.AccountTypeName = account.AccountTypeLookup?.DisplayName ?? account.AccountTypeLookup?.Name ?? account.AccountType;
            dto.Balance = await _accountBalanceManager.GetBalanceAsync(dto.Id);
            return dto;
        }

        public override async Task<PagedResultDto<BusinessAccountDto>> GetAllAsync(PagedBusinessAccountResultRequestDto input)
        {
            await _systemAccountManager.EnsureSystemAccountsAsync();

            var query = CreateFilteredQuery(input);
            var totalCount = await AsyncQueryableExecuter.CountAsync(query);

            query = ApplySorting(query, input);
            query = ApplyPaging(query, input);

            var entities = await AsyncQueryableExecuter.ToListAsync(query);
            var dtos = new List<BusinessAccountDto>();

            foreach (var entity in entities)
            {
                await _accountBalanceManager.EnsureOpeningBalancePostedAsync(entity);
                var dto = ObjectMapper.Map<BusinessAccountDto>(entity);
                dto.AccountTypeName = entity.AccountTypeLookup?.DisplayName ?? entity.AccountTypeLookup?.Name ?? entity.AccountType;
                dto.Balance = await _accountBalanceManager.GetBalanceAsync(dto.Id);
                dtos.Add(dto);
            }
            await CurrentUnitOfWork.SaveChangesAsync();

            return new PagedResultDto<BusinessAccountDto>(totalCount, dtos);
        }

        protected override IQueryable<BusinessAccount> CreateFilteredQuery(PagedBusinessAccountResultRequestDto input)
        {
            return Repository.GetAll()
                .Include(x => x.AccountTypeLookup)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Code != null && x.Code.Contains(input.Keyword))
                         || (x.AccountType != null && x.AccountType.Contains(input.Keyword))
                         || (x.AccountTypeLookup != null && x.AccountTypeLookup.DisplayName.Contains(input.Keyword))
                         || (x.AccountTypeLookup != null && x.AccountTypeLookup.Name.Contains(input.Keyword))
                         || (x.Description != null && x.Description.Contains(input.Keyword)))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive.Value);
        }
    }
}
