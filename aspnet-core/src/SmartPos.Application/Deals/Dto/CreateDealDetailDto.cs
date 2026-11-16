using System;
using System.ComponentModel.DataAnnotations;

namespace SmartPos.Deals.Dto
{
    public class CreateDealDetailDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        /// <summary>Display labels for pre-selected options (e.g. "Size: Small").</summary>
        [StringLength(1024)]
        public string SelectedModifiers { get; set; }

        /// <summary>Comma-separated ProductModifierOption ids.</summary>
        [StringLength(512)]
        public string SelectedOptionIds { get; set; }
    }
}
