using System.Collections.Generic;

namespace SmartPos.HostCatalog.Dto
{
    public class HostCatalogByCompanyTypeDto
    {
        public HostCatalogItemDto CompanyType { get; set; }

        public List<HostCatalogItemDto> Categories { get; set; } = new List<HostCatalogItemDto>();

        public List<HostCatalogItemDto> Units { get; set; } = new List<HostCatalogItemDto>();

        public List<HostCatalogItemDto> Brands { get; set; } = new List<HostCatalogItemDto>();
    }
}
