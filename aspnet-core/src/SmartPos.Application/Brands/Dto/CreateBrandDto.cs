using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Brands;

namespace SmartPos.Brands.Dto
{
    [AutoMapTo(typeof(Brand))]
    public class CreateBrandDto
    {
        [Required]
        [StringLength(Brand.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Brand.MaxDescriptionLength)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
