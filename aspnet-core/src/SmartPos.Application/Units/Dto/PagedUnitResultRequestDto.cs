using Abp.Application.Services.Dto;

namespace SmartPos.Units.Dto
{
    public class PagedUnitResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
    }
}
