using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.HostCatalog;

namespace SmartPos.HostCatalog.Dto
{
    [AutoMapFrom(typeof(HostCatalogItem))]
    [AutoMapTo(typeof(HostCatalogItem))]
    public class HostCatalogItemDto : EntityDto
    {
        [Required]
        [StringLength(HostCatalogItem.MaxTypeLength)]
        public string Type { get; set; }

        public int? CompanyTypeId { get; set; }

        public string CompanyTypeName { get; set; }

        [Required]
        [StringLength(HostCatalogItem.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(HostCatalogItem.MaxSymbolLength)]
        public string Symbol { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
