using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;

namespace SmartPos.Accounts
{
    public class SystemAccountManager : DomainService
    {
        private readonly IRepository<BusinessAccount> _accountRepository;

        public SystemAccountManager(IRepository<BusinessAccount> accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task EnsureSystemAccountsAsync()
        {
            await GetOrCreateAsync(SystemAccountCodes.Cash, "Cash Account", AccountTypes.Cash);
            await GetOrCreateAsync(SystemAccountCodes.Bank, "Bank Account", AccountTypes.Bank);
            await GetOrCreateAsync(SystemAccountCodes.Purchase, "Purchase Account", AccountTypes.Purchase);
            await GetOrCreateAsync(SystemAccountCodes.Sale, "Sale Account", AccountTypes.Sale);
            await GetOrCreateAsync(SystemAccountCodes.Expense, "Expense Account", AccountTypes.Expense);
        }

        public Task<BusinessAccount> GetPurchaseAccountAsync()
        {
            return GetOrCreateAsync(SystemAccountCodes.Purchase, "Purchase Account", AccountTypes.Purchase);
        }

        public Task<BusinessAccount> GetSaleAccountAsync()
        {
            return GetOrCreateAsync(SystemAccountCodes.Sale, "Sale Account", AccountTypes.Sale);
        }

        public Task<BusinessAccount> GetExpenseAccountAsync()
        {
            return GetOrCreateAsync(SystemAccountCodes.Expense, "Expense Account", AccountTypes.Expense);
        }

        public Task<BusinessAccount> GetCashAccountAsync()
        {
            return GetOrCreateAsync(SystemAccountCodes.Cash, "Cash Account", AccountTypes.Cash);
        }

        public Task<BusinessAccount> GetBankAccountAsync()
        {
            return GetOrCreateAsync(SystemAccountCodes.Bank, "Bank Account", AccountTypes.Bank);
        }

        private async Task<BusinessAccount> GetOrCreateAsync(string code, string name, string accountType)
        {
            var account = await _accountRepository.FirstOrDefaultAsync(x => x.Code == code);
            if (account != null)
            {
                return account;
            }

            account = new BusinessAccount
            {
                Name = name,
                Code = code,
                AccountType = accountType,
                OpeningBalance = 0,
                Description = "System account",
                IsActive = true
            };

            await _accountRepository.InsertAsync(account);
            await CurrentUnitOfWork.SaveChangesAsync();
            return account;
        }
    }
}
