using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;

namespace SmartPos.Branches.Dto
{
    [AutoMapTo(typeof(Branch))]
    public class CreateBranchDto
    {
        [Required]
        [StringLength(Branch.MaxNameLength)]
        public string Name { get; set; }

        [Required]
        [StringLength(Branch.MaxCodeLength)]
        public string Code { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
