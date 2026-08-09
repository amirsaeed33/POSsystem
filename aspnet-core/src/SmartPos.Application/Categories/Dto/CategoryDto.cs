using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Categories;

namespace SmartPos.Categories.Dto
{
    [AutoMapFrom(typeof(Category))]
    [AutoMapTo(typeof(Category))]
    public class CategoryDto : EntityDto
    {
        public int BranchId { get; set; }

        [Required]
        [StringLength(Category.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Category.MaxDescriptionLength)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int? HostSourceId { get; set; }
    }
}
