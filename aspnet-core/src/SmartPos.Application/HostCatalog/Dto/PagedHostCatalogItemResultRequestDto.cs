using Abp.Application.Services.Dto;

namespace SmartPos.HostCatalog.Dto
{
    public class PagedHostCatalogItemResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public string Type { get; set; }

        public int? CompanyTypeId { get; set; }

        public bool? IsActive { get; set; }
    }
}
