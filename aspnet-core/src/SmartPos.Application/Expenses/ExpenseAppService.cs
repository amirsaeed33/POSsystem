using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using SmartPos.Accounts;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Expenses.Dto;

namespace SmartPos.Expenses
{
    [AbpAuthorize(PermissionNames.Pages_Expenses)]
    public class ExpenseAppService : AsyncCrudAppService<Expense, ExpenseDto, int, PagedExpenseResultRequestDto, CreateExpenseDto, ExpenseDto>, IExpenseAppService
    {
        private readonly IRepository<BusinessAccount> _accountRepository;
        private readonly IRepository<LedgerEntry> _ledgerRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly SystemAccountManager _systemAccountManager;

        public ExpenseAppService(
            IRepository<Expense> repository,
            IRepository<BusinessAccount> accountRepository,
            IRepository<LedgerEntry> ledgerRepository,
            IRepository<User, long> userRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            SystemAccountManager systemAccountManager)
            : base(repository)
        {
            _accountRepository = accountRepository;
            _ledgerRepository = ledgerRepository;
            _userRepository = userRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _systemAccountManager = systemAccountManager;
        }

        public override async Task<ExpenseDto> CreateAsync(CreateExpenseDto input)
        {
            CheckCreatePermission();

            if (input.Amount <= 0)
            {
                throw new UserFriendlyException("Amount must be greater than zero.");
            }

            var paymentAccount = await _accountRepository.GetAsync(input.PaymentAccountId);
            var expenseAccount = await _systemAccountManager.GetExpenseAccountAsync();

            if (input.ExpenseDate == default)
            {
                input.ExpenseDate = Abp.Timing.Clock.Now;
            }

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();

            var expense = new Expense
            {
                TenantId = AbpSession.TenantId,
                BranchId = branchId,
                ExpenseDate = input.ExpenseDate,
                Amount = input.Amount,
                ReferenceNo = input.ReferenceNo,
                Description = input.Description,
                PaymentAccountId = paymentAccount.Id,
                ExpenseAccountId = expenseAccount.Id
            };

            await Repository.InsertAsync(expense);
            await CurrentUnitOfWork.SaveChangesAsync();

            var description = "Expense " + (expense.ReferenceNo.IsNullOrWhiteSpace() ? "#" + expense.Id : expense.ReferenceNo);

            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = expenseAccount.Id,
                TransactionDate = expense.ExpenseDate,
                VoucherType = VoucherTypes.Expense,
                VoucherId = expense.Id,
                Debit = expense.Amount,
                Credit = 0,
                Description = description
            });

            await _ledgerRepository.InsertAsync(new LedgerEntry
            {
                AccountId = paymentAccount.Id,
                TransactionDate = expense.ExpenseDate,
                VoucherType = VoucherTypes.Expense,
                VoucherId = expense.Id,
                Debit = 0,
                Credit = expense.Amount,
                Description = description
            });

            return await GetAsync(new EntityDto<int>(expense.Id));
        }

        public override async Task<ExpenseDto> UpdateAsync(ExpenseDto input)
        {
            throw new UserFriendlyException("Expenses cannot be edited. Delete and create a new expense instead.");
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var expense = await Repository.GetAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(expense.BranchId);

            var ledgerEntries = await _ledgerRepository.GetAllListAsync(
                x => x.VoucherType == VoucherTypes.Expense && x.VoucherId == expense.Id);
            foreach (var entry in ledgerEntries)
            {
                await _ledgerRepository.DeleteAsync(entry);
            }

            await Repository.DeleteAsync(expense);
        }

        protected override IQueryable<Expense> CreateFilteredQuery(PagedExpenseResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            return Repository.GetAllIncluding(x => x.PaymentAccount, x => x.ExpenseAccount)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(input.PaymentAccountId.HasValue, x => x.PaymentAccountId == input.PaymentAccountId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.Description != null && x.Description.Contains(input.Keyword))
                         || (x.ReferenceNo != null && x.ReferenceNo.Contains(input.Keyword))
                         || (x.PaymentAccount != null && x.PaymentAccount.Name.Contains(input.Keyword)));
        }

        protected override async Task<Expense> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.PaymentAccount, x => x.ExpenseAccount)
                    .Where(x => x.Id == id));
        }

        protected override ExpenseDto MapToEntityDto(Expense entity)
        {
            var dto = base.MapToEntityDto(entity);
            dto.PaymentAccountName = entity.PaymentAccount?.Name;
            dto.ExpenseAccountName = entity.ExpenseAccount?.Name;
            if (entity.CreatorUserId.HasValue)
            {
                var user = _userRepository.FirstOrDefault(entity.CreatorUserId.Value);
                if (user != null)
                {
                    dto.CreatedByName = (user.Name + " " + user.Surname).Trim();
                }
            }
            return dto;
        }
    }
}
