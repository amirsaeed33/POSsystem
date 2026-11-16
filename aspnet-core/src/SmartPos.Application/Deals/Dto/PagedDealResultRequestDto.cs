using Abp.Application.Services.Dto;

namespace SmartPos.Deals.Dto
{
    public class PagedDealResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? BranchId { get; set; }
    }
}
