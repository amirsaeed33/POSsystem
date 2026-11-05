using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Categories;

namespace SmartPos.Categories.Dto
{
    [AutoMapTo(typeof(Category))]
    public class CreateCategoryDto
    {
        public int? DepartmentId { get; set; }

        [Required]
        [StringLength(Category.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Category.MaxDescriptionLength)]
        public string Description { get; set; }

        /// <summary>Optional new image as data URL (data:image/...;base64,...).</summary>
        public string ImageBase64 { get; set; }

        public bool IsActive { get; set; } = true;

        public int? DefaultUnitId { get; set; }
    }
}
