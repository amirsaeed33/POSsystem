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
using SmartPos.Customers.Dto;

namespace SmartPos.Customers
{
    [AbpAuthorize(PermissionNames.Pages_Customers)]
    public class CustomerAppService : AsyncCrudAppService<Customer, CustomerDto, int, PagedCustomerResultRequestDto, CreateCustomerDto, CustomerDto>, ICustomerAppService
    {
        private readonly IRepository<BusinessAccount> _accountRepository;
        private readonly AccountBalanceManager _accountBalanceManager;

        public CustomerAppService(
            IRepository<Customer> repository,
            IRepository<BusinessAccount> accountRepository,
            AccountBalanceManager accountBalanceManager)
            : base(repository)
        {
            _accountRepository = accountRepository;
            _accountBalanceManager = accountBalanceManager;
        }

        public override async Task<CustomerDto> CreateAsync(CreateCustomerDto input)
        {
            CheckCreatePermission();

            var account = new BusinessAccount
            {
                Name = input.Name,
                Code = "CUS-" + Abp.Timing.Clock.Now.ToString("yyyyMMddHHmmss"),
                AccountType = AccountTypes.Customer,
                OpeningBalance = 0,
                Description = "Customer account: " + input.Name,
                IsActive = true
            };

            await _accountRepository.InsertAsync(account);
            await CurrentUnitOfWork.SaveChangesAsync();

            var customer = ObjectMapper.Map<Customer>(input);
            customer.AccountId = account.Id;

            await Repository.InsertAsync(customer);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(customer.Id));
        }

        public override async Task<CustomerDto> UpdateAsync(CustomerDto input)
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

        public override async Task<CustomerDto> GetAsync(EntityDto<int> input)
        {
            await EnsureLinkedAccountAsync(input.Id);
            var dto = await base.GetAsync(input);
            await FillBalance(dto);
            return dto;
        }

        public override async Task<PagedResultDto<CustomerDto>> GetAllAsync(PagedCustomerResultRequestDto input)
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

            var customer = await Repository.GetAsync(input.Id);
            var accountId = customer.AccountId;

            await Repository.DeleteAsync(customer);

            if (accountId.HasValue)
            {
                await _accountRepository.DeleteAsync(accountId.Value);
            }
        }

        protected override IQueryable<Customer> CreateFilteredQuery(PagedCustomerResultRequestDto input)
        {
            return Repository.GetAllIncluding(x => x.Account)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Phone != null && x.Phone.Contains(input.Keyword))
                         || (x.Email != null && x.Email.Contains(input.Keyword))
                         || (x.Address != null && x.Address.Contains(input.Keyword))
                         || (x.Description != null && x.Description.Contains(input.Keyword)));
        }

        protected override async Task<Customer> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Account).Where(x => x.Id == id));
        }

        private async Task FillBalance(CustomerDto dto)
        {
            if (dto.AccountId.HasValue)
            {
                dto.Balance = await _accountBalanceManager.GetBalanceAsync(dto.AccountId.Value);
            }
        }

        private async Task BackfillMissingAccountsAsync()
        {
            var missing = await Repository.GetAllListAsync(x => x.AccountId == null);
            foreach (var customer in missing)
            {
                await CreateLinkedAccountAsync(customer);
            }
        }

        private async Task EnsureLinkedAccountAsync(int customerId)
        {
            var customer = await Repository.GetAsync(customerId);
            if (!customer.AccountId.HasValue)
            {
                await CreateLinkedAccountAsync(customer);
            }
        }

        private async Task CreateLinkedAccountAsync(Customer customer)
        {
            var account = new BusinessAccount
            {
                Name = customer.Name,
                Code = "CUS-" + customer.Id,
                AccountType = AccountTypes.Customer,
                OpeningBalance = 0,
                Description = "Customer account: " + customer.Name,
                IsActive = true
            };

            await _accountRepository.InsertAsync(account);
            await CurrentUnitOfWork.SaveChangesAsync();

            customer.AccountId = account.Id;
            await CurrentUnitOfWork.SaveChangesAsync();
        }
    }
}
