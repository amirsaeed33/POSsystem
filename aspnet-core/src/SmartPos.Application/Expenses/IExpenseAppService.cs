using Abp.Application.Services;
using SmartPos.Expenses.Dto;

namespace SmartPos.Expenses
{
    public interface IExpenseAppService : IAsyncCrudAppService<ExpenseDto, int, PagedExpenseResultRequestDto, CreateExpenseDto, ExpenseDto>
    {
    }
}
