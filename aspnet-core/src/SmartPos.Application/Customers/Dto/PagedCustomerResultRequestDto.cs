using Abp.Application.Services.Dto;

namespace SmartPos.Customers.Dto
{
    public class PagedCustomerResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
    }
}
