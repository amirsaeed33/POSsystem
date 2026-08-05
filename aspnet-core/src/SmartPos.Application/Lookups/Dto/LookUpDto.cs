using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Lookups;

namespace SmartPos.Lookups.Dto
{
    [AutoMapFrom(typeof(LookUp))]
    [AutoMapTo(typeof(LookUp))]
    public class LookUpDto : EntityDto
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

        public bool IsActive { get; set; }
    }
}
