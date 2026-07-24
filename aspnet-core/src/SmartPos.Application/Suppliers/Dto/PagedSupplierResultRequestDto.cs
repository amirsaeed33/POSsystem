using Abp.Application.Services.Dto;

namespace SmartPos.Suppliers.Dto
{
    public class PagedSupplierResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
    }
}
