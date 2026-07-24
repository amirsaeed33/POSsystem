using Abp.Application.Services.Dto;

namespace SmartPos.Purchases.Dto
{
    public class PagedPurchaseResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? SupplierId { get; set; }
    }
}
