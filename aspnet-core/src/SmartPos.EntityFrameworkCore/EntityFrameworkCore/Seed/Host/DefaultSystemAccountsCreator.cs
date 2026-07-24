using System.Linq;
using SmartPos.Accounts;

namespace SmartPos.EntityFrameworkCore.Seed.Host
{
    public class DefaultSystemAccountsCreator
    {
        private readonly SmartPosDbContext _context;
        private readonly int? _tenantId;

        public DefaultSystemAccountsCreator(SmartPosDbContext context, int? tenantId)
        {
            _context = context;
            _tenantId = tenantId;
        }

        public void Create()
        {
            AddIfMissing(SystemAccountCodes.Cash, "Cash Account", AccountTypes.Cash);
            AddIfMissing(SystemAccountCodes.Bank, "Bank Account", AccountTypes.Bank);
            AddIfMissing(SystemAccountCodes.Purchase, "Purchase Account", AccountTypes.Purchase);
            AddIfMissing(SystemAccountCodes.Sale, "Sale Account", AccountTypes.Sale);
            AddIfMissing(SystemAccountCodes.Expense, "Expense Account", AccountTypes.Expense);
            _context.SaveChanges();
        }

        private void AddIfMissing(string code, string name, string accountType)
        {
            var exists = _context.Accounts.Any(x => x.TenantId == _tenantId && x.Code == code && !x.IsDeleted);
            if (exists)
            {
                return;
            }

            _context.Accounts.Add(new BusinessAccount
            {
                TenantId = _tenantId,
                Name = name,
                Code = code,
                AccountType = accountType,
                OpeningBalance = 0,
                Description = "System account",
                IsActive = true
            });
        }
    }
}
