using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using SmartPos.Authorization;
using SmartPos.Accounts.Dto;

namespace SmartPos.Accounts
{
    [AbpAuthorize(PermissionNames.Pages_LedgerEntries)]
    public class LedgerEntryAppService : AsyncCrudAppService<LedgerEntry, LedgerEntryDto, int, PagedLedgerEntryResultRequestDto, CreateLedgerEntryDto, LedgerEntryDto>, ILedgerEntryAppService
    {
        private readonly IRepository<BusinessAccount> _accountRepository;
        private readonly AccountBalanceManager _accountBalanceManager;

        public LedgerEntryAppService(
            IRepository<LedgerEntry> repository,
            IRepository<BusinessAccount> accountRepository,
            AccountBalanceManager accountBalanceManager)
            : base(repository)
        {
            _accountRepository = accountRepository;
            _accountBalanceManager = accountBalanceManager;
        }

        public async Task<AccountBalanceDto> GetBalance(EntityDto input)
        {
            var account = await _accountRepository.GetAsync(input.Id);
            return new AccountBalanceDto
            {
                AccountId = account.Id,
                AccountName = account.Name,
                AccountType = account.AccountType,
                Balance = await _accountBalanceManager.GetBalanceAsync(account.Id)
            };
        }

        public override async Task<LedgerEntryDto> CreateAsync(CreateLedgerEntryDto input)
        {
            ValidateAmounts(input.Debit, input.Credit);
            await EnsureAccountExists(input.AccountId);

            if (input.TransactionDate == default)
            {
                input.TransactionDate = Abp.Timing.Clock.Now;
            }

            return await base.CreateAsync(input);
        }

        public override async Task<LedgerEntryDto> UpdateAsync(LedgerEntryDto input)
        {
            ValidateAmounts(input.Debit, input.Credit);
            await EnsureAccountExists(input.AccountId);
            return await base.UpdateAsync(input);
        }

        protected override IQueryable<LedgerEntry> CreateFilteredQuery(PagedLedgerEntryResultRequestDto input)
        {
            return Repository.GetAllIncluding(x => x.Account)
                .WhereIf(input.AccountId.HasValue, x => x.AccountId == input.AccountId.Value)
                .WhereIf(!input.VoucherType.IsNullOrWhiteSpace(), x => x.VoucherType == input.VoucherType)
                .WhereIf(input.FromDate.HasValue, x => x.TransactionDate >= input.FromDate.Value)
                .WhereIf(input.ToDate.HasValue, x => x.TransactionDate <= input.ToDate.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.Description != null && x.Description.Contains(input.Keyword))
                         || (x.VoucherType != null && x.VoucherType.Contains(input.Keyword))
                         || (x.Account != null && x.Account.Name.Contains(input.Keyword)));
        }

        protected override async Task<LedgerEntry> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Account).Where(x => x.Id == id));
        }

        protected override LedgerEntryDto MapToEntityDto(LedgerEntry entity)
        {
            var dto = base.MapToEntityDto(entity);
            dto.AccountName = entity.Account?.Name;
            return dto;
        }

        private async Task EnsureAccountExists(int accountId)
        {
            var exists = await _accountRepository.FirstOrDefaultAsync(accountId);
            if (exists == null)
            {
                throw new UserFriendlyException("Account not found.");
            }
        }

        private static void ValidateAmounts(decimal debit, decimal credit)
        {
            if (debit < 0 || credit < 0)
            {
                throw new UserFriendlyException("Debit and Credit cannot be negative.");
            }

            if (debit == 0 && credit == 0)
            {
                throw new UserFriendlyException("Enter a Debit or Credit amount.");
            }

            if (debit > 0 && credit > 0)
            {
                throw new UserFriendlyException("A ledger line can have Debit or Credit, not both.");
            }
        }
    }
}
