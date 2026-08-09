using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Brands;

namespace SmartPos.Brands.Dto
{
    [AutoMapFrom(typeof(Brand))]
    [AutoMapTo(typeof(Brand))]
    public class BrandDto : EntityDto
    {
        public int BranchId { get; set; }

        [Required]
        [StringLength(Brand.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Brand.MaxDescriptionLength)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int? HostSourceId { get; set; }
    }
}
