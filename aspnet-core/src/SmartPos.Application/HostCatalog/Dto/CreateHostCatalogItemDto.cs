using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.HostCatalog;

namespace SmartPos.HostCatalog.Dto
{
    [AutoMapTo(typeof(HostCatalogItem))]
    public class CreateHostCatalogItemDto
    {
        [Required]
        [StringLength(HostCatalogItem.MaxTypeLength)]
        public string Type { get; set; }

        public int? CompanyTypeId { get; set; }

        [Required]
        [StringLength(HostCatalogItem.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(HostCatalogItem.MaxSymbolLength)]
        public string Symbol { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
