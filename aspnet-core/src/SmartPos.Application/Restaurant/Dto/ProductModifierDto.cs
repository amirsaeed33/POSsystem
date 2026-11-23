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

        /// <summary>True when admin left price empty (included in sale; no +extra).</summary>
        public bool IsIncludedInBasePrice { get; set; }

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

        /// <summary>True when admin left the price box empty.</summary>
        public bool IsIncludedInBasePrice { get; set; } = false;

        public bool IsDefault { get; set; } = false;
    }

    public class UpdateModifierOptionDto : EntityDto
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        public decimal Price { get; set; } = 0;

        /// <summary>True when option uses product sale price (no +extra).</summary>
        public bool IsIncludedInBasePrice { get; set; } = false;

        public bool IsDefault { get; set; } = false;
    }

    /// <summary>Branch-level reusable modifier template (applied to new/empty products).</summary>
    public class BranchModifierTemplateDto
    {
        public int BranchId { get; set; }

        public string GroupName { get; set; } = "Size";

        public bool IsRequired { get; set; } = true;

        public bool AllowMultiple { get; set; } = false;

        public List<BranchModifierTemplateOptionDto> Options { get; set; } = new List<BranchModifierTemplateOptionDto>();
    }

    public class BranchModifierTemplateOptionDto
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>When true, option uses the product sale price (included, no +extra).</summary>
        public bool UseSalePrice { get; set; }

        /// <summary>Add-on / sale amount when <see cref="UseSalePrice"/> is false.</summary>
        public decimal Price { get; set; }

        public bool IsDefault { get; set; }
    }

    public class ApplyModifierTemplateInput
    {
        [Required]
        public int ProductId { get; set; }

        public int? BranchId { get; set; }
    }
}
