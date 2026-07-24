using Abp.Application.Services.Dto;

namespace SmartPos.Sales.Dto
{
    public class PagedSaleResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? CustomerId { get; set; }
    }
}
