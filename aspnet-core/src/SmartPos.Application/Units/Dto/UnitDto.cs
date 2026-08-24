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
        public int BranchId { get; set; }

        [Required]
        [StringLength(Unit.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Unit.MaxDescriptionLength)]
        public string Description { get; set; }

        [StringLength(Unit.MaxSymbolLength)]
        public string Symbol { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
