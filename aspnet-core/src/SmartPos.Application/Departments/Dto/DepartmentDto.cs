using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace SmartPos.Departments.Dto
{
    [AutoMapFrom(typeof(Department))]
    [AutoMapTo(typeof(Department))]
    public class DepartmentDto : EntityDto
    {
        public int BranchId { get; set; }

        [Required]
        [StringLength(Department.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Department.MaxDescriptionLength)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

