using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;

namespace SmartPos.Departments.Dto
{
    [AutoMapTo(typeof(Department))]
    public class CreateDepartmentDto
    {
        [Required]
        [StringLength(Department.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Department.MaxDescriptionLength)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

