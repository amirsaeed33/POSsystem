using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Categories;

namespace SmartPos.Categories.Dto
{
    [AutoMapTo(typeof(Category))]
    public class CreateCategoryDto
    {
        [Required]
        [StringLength(Category.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Category.MaxDescriptionLength)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
