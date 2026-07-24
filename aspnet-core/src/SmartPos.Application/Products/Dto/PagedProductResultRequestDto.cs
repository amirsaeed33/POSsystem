using Abp.Application.Services.Dto;

namespace SmartPos.Products.Dto
{
    public class PagedProductResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? CategoryId { get; set; }

        public int? BrandId { get; set; }

        public int? UnitId { get; set; }
    }
}
