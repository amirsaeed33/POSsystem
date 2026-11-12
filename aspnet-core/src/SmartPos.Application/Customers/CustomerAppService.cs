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
using SmartPos.Addresses;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Accounts;
using SmartPos.Branches;
using SmartPos.Customers.Dto;
using SmartPos.Orders;
using SmartPos.Restaurant;

namespace SmartPos.Customers
{
    [AbpAuthorize(PermissionNames.Pages_Customers)]
    public class CustomerAppService : AsyncCrudAppService<Customer, CustomerDto, int, PagedCustomerResultRequestDto, CreateCustomerDto, CustomerDto>, ICustomerAppService
    {
        private readonly IRepository<BusinessAccount> _accountRepository;
        private readonly IRepository<CustomerOrder> _customerOrderRepository;
        private readonly IRepository<RestaurantOrder> _restaurantOrderRepository;
        private readonly SavedAddressManager _savedAddresses;
        private readonly IRepository<User, long> _userRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly AccountBalanceManager _accountBalanceManager;

        public CustomerAppService(
            IRepository<Customer> repository,
            IRepository<BusinessAccount> accountRepository,
            IRepository<CustomerOrder> customerOrderRepository,
            IRepository<RestaurantOrder> restaurantOrderRepository,
            SavedAddressManager savedAddresses,
            IRepository<User, long> userRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            AccountBalanceManager accountBalanceManager)
            : base(repository)
        {
            _accountRepository = accountRepository;
            _customerOrderRepository = customerOrderRepository;
            _restaurantOrderRepository = restaurantOrderRepository;
            _savedAddresses = savedAddresses;
            _userRepository = userRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _accountBalanceManager = accountBalanceManager;
            CreatePermissionName = PermissionNames.Pages_Customers_Create;
            UpdatePermissionName = PermissionNames.Pages_Customers_Edit;
            DeletePermissionName = PermissionNames.Pages_Customers_Delete;
            GetPermissionName = PermissionNames.Pages_Customers;
            GetAllPermissionName = PermissionNames.Pages_Customers;
        }

        public override async Task<CustomerDto> CreateAsync(CreateCustomerDto input)
        {
            CheckCreatePermission();

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();

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
            customer.BranchId = branchId;
            customer.AccountId = account.Id;

            await Repository.InsertAsync(customer);
            await CurrentUnitOfWork.SaveChangesAsync();

            await _savedAddresses.UpsertDefaultAsync(
                SavedAddressOwnerTypes.Customer,
                customer.Id,
                customer.TenantId,
                input.Address);

            return await GetAsync(new EntityDto<int>(customer.Id));
        }

        public override async Task<CustomerDto> UpdateAsync(CustomerDto input)
        {
            var customer = await Repository.GetAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(customer.BranchId);

            var branchId = customer.BranchId;
            MapToEntity(input, customer);
            customer.BranchId = branchId;

            await CurrentUnitOfWork.SaveChangesAsync();

            if (customer.AccountId.HasValue)
            {
                var account = await _accountRepository.GetAsync(customer.AccountId.Value);
                account.Name = input.Name;
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            await _savedAddresses.UpsertDefaultAsync(
                SavedAddressOwnerTypes.Customer,
                customer.Id,
                customer.TenantId,
                input.Address);

            return await GetAsync(new EntityDto<int>(customer.Id));
        }

        public override async Task<CustomerDto> GetAsync(EntityDto<int> input)
        {
            await EnsureLinkedAccountAsync(input.Id);
            var dto = await base.GetAsync(input);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(dto.BranchId);
            await FillBalance(dto);
            dto.Address = await _savedAddresses.GetDefaultAddressTextAsync(
                SavedAddressOwnerTypes.Customer, dto.Id);
            await FillOnlineOrderCountsAsync(new[] { dto });
            return dto;
        }

        public override async Task<PagedResultDto<CustomerDto>> GetAllAsync(PagedCustomerResultRequestDto input)
        {
            await BackfillMissingAccountsAsync();
            var result = await base.GetAllAsync(input);
            foreach (var item in result.Items)
            {
                await FillBalance(item);
                item.Address = await _savedAddresses.GetDefaultAddressTextAsync(
                    SavedAddressOwnerTypes.Customer, item.Id);
            }
            await FillOnlineOrderCountsAsync(result.Items);
            return result;
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var customer = await Repository.GetAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(customer.BranchId);
            var accountId = customer.AccountId;

            await Repository.DeleteAsync(customer);

            if (accountId.HasValue)
            {
                await _accountRepository.DeleteAsync(accountId.Value);
            }
        }

        protected override IQueryable<Customer> CreateFilteredQuery(PagedCustomerResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);
            var keyword = input.Keyword;

            return Repository.GetAllIncluding(x => x.Account)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(!keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(keyword)
                         || (x.Phone != null && x.Phone.Contains(keyword))
                         || (x.Email != null && x.Email.Contains(keyword))
                         || (x.Description != null && x.Description.Contains(keyword))
                         || _savedAddresses.QueryAll().Any(a =>
                             a.OwnerType == SavedAddressOwnerTypes.Customer &&
                             a.OwnerId == x.Id &&
                             a.Address != null &&
                             a.Address.Contains(keyword)));
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

        private async Task FillOnlineOrderCountsAsync(IReadOnlyList<CustomerDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var ids = items.Select(x => x.Id).Distinct().ToList();

            var storefrontCounts = await _customerOrderRepository.GetAll()
                .AsNoTracking()
                .Where(o => ids.Contains(o.CustomerId))
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToListAsync();

            var posOnlineCounts = await _restaurantOrderRepository.GetAll()
                .AsNoTracking()
                .Where(o =>
                    o.CustomerId.HasValue &&
                    ids.Contains(o.CustomerId.Value) &&
                    o.OrderType == OrderType.Delivery)
                .GroupBy(o => o.CustomerId.Value)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToListAsync();

            var totals = new Dictionary<int, int>();
            foreach (var row in storefrontCounts)
            {
                totals[row.CustomerId] = row.Count;
            }
            foreach (var row in posOnlineCounts)
            {
                totals[row.CustomerId] = totals.TryGetValue(row.CustomerId, out var existing)
                    ? existing + row.Count
                    : row.Count;
            }

            foreach (var item in items)
            {
                item.OnlineOrderCount = totals.TryGetValue(item.Id, out var count) ? count : 0;
            }
        }

        private async Task BackfillMissingAccountsAsync()
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);
            var missing = await Repository.GetAllListAsync(x =>
                x.AccountId == null && (!branchId.HasValue || x.BranchId == branchId.Value));
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
