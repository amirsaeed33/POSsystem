using Abp.Application.Services.Dto;

namespace SmartPos.Expenses.Dto
{
    public class PagedExpenseResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? PaymentAccountId { get; set; }
    }
}
