using Abp.Application.Services.Dto;

namespace SmartPos.CompanyProfiles.Dto
{
    public class PagedCompanyProfileResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
    }
}
