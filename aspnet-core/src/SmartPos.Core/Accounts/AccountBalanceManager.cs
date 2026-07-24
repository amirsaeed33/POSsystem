using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;

namespace SmartPos.Accounts
{
    public class AccountBalanceManager : DomainService
    {
        private readonly IRepository<LedgerEntry> _ledgerRepository;

        public AccountBalanceManager(IRepository<LedgerEntry> ledgerRepository)
        {
            _ledgerRepository = ledgerRepository;
        }

        public async Task<decimal> GetBalanceAsync(int accountId)
        {
            var entries = await _ledgerRepository.GetAllListAsync(x => x.AccountId == accountId);
            return entries.Sum(x => x.Debit) - entries.Sum(x => x.Credit);
        }

        public async Task InsertOpeningBalanceAsync(int accountId, decimal openingBalance, string description = null)
        {
            if (openingBalance == 0)
            {
                return;
            }

            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = accountId,
                TransactionDate = Abp.Timing.Clock.Now,
                VoucherType = VoucherTypes.OpeningBalance,
                Debit = openingBalance > 0 ? openingBalance : 0,
                Credit = openingBalance < 0 ? -openingBalance : 0,
                Description = description ?? "Opening balance"
            });
        }

        public async Task EnsureOpeningBalancePostedAsync(BusinessAccount account)
        {
            if (account.OpeningBalance == 0)
            {
                return;
            }

            var count = await _ledgerRepository.CountAsync(
                x => x.AccountId == account.Id && x.VoucherType == VoucherTypes.OpeningBalance);

            if (count == 0)
            {
                await InsertOpeningBalanceAsync(account.Id, account.OpeningBalance);
            }
        }
    }
}
