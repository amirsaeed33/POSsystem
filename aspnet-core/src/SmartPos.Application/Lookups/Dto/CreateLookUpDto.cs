using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Lookups;

namespace SmartPos.Lookups.Dto
{
    [AutoMapTo(typeof(LookUp))]
    public class CreateLookUpDto
    {
        [Required]
        [StringLength(LookUp.MaxTypeLength)]
        public string Type { get; set; }

        [Required]
        [StringLength(LookUp.MaxNameLength)]
        public string Name { get; set; }

        [Required]
        [StringLength(LookUp.MaxDisplayNameLength)]
        public string DisplayName { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
