using Abp.Application.Services.Dto;

namespace SmartPos.Categories.Dto
{
    public class PagedCategoryResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
    }
}
