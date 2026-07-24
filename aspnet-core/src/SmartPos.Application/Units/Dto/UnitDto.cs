using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Units;

namespace SmartPos.Units.Dto
{
    [AutoMapFrom(typeof(Unit))]
    [AutoMapTo(typeof(Unit))]
    public class UnitDto : EntityDto
    {
        [Required]
        [StringLength(Unit.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Unit.MaxDescriptionLength)]
        public string Description { get; set; }
    }
}
