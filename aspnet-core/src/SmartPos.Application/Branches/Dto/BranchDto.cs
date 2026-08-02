using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace SmartPos.Branches.Dto
{
    [AutoMapFrom(typeof(Branch))]
    [AutoMapTo(typeof(Branch))]
    public class BranchDto : EntityDto
    {
        [Required]
        [StringLength(Branch.MaxNameLength)]
        public string Name { get; set; }

        [Required]
        [StringLength(Branch.MaxCodeLength)]
        public string Code { get; set; }

        public bool IsActive { get; set; }

        public bool IsDefault { get; set; }
    }
}
