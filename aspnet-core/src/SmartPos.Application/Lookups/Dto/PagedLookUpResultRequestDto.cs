using Abp.Application.Services.Dto;

namespace SmartPos.Lookups.Dto
{
    public class PagedLookUpResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public string Type { get; set; }

        public bool? IsActive { get; set; }
    }
}
