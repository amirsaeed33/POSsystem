using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Units;

namespace SmartPos.Units.Dto
{
    [AutoMapTo(typeof(Unit))]
    public class CreateUnitDto
    {
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
