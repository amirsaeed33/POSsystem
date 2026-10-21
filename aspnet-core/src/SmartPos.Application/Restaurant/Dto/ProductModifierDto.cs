using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace SmartPos.Restaurant.Dto
{
    public class ProductModifierGroupDto : EntityDto
    {
        public int ProductId { get; set; }

        public string Name { get; set; }

        public int MinSelection { get; set; }

        public int MaxSelection { get; set; }

        public bool IsRequired { get; set; }

        public List<ProductModifierOptionDto> Options { get; set; } = new List<ProductModifierOptionDto>();
    }

    public class ProductModifierOptionDto : EntityDto
    {
        public int ModifierGroupId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public bool IsDefault { get; set; }
    }

    public class CreateModifierGroupDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        public int MinSelection { get; set; } = 0;

        public int MaxSelection { get; set; } = 1;

        public bool IsRequired { get; set; } = false;

        public List<CreateModifierOptionDto> Options { get; set; } = new List<CreateModifierOptionDto>();
    }

    public class CreateModifierOptionDto
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        public decimal Price { get; set; } = 0;

        public bool IsDefault { get; set; } = false;
    }
}
