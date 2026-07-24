using Abp.Application.Services.Dto;

namespace SmartPos.Brands.Dto
{
    public class PagedBrandResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
    }
}
