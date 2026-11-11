using Abp.Application.Services.Dto;

namespace SmartPos.Sellers.Dto
{
    public class PagedSellerResultRequestDto : PagedAndSortedResultRequestDto
    {
        public string Keyword { get; set; }

        public SellerStatus? Status { get; set; }
    }
}
