using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartPos.Deals;

namespace SmartPos.Deals.Dto
{
    public class CreateDealDto
    {
        public int? BranchId { get; set; }

        [Required(ErrorMessage = "Deal name is required.")]
        [StringLength(Deal.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Deal.MaxDescriptionLength)]
        public string Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Deal price must be greater than zero.")]
        public decimal DealPrice { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int? CategoryId { get; set; }

        [StringLength(Deal.MaxCategoryTypeLength)]
        public string CategoryType { get; set; } = "Deals";

        /// <summary>
        /// Optional new image as data URL (data:image/...;base64,...).
        /// Saved onto the linked deal product.
        /// </summary>
        public string ImageBase64 { get; set; }

        [Required(ErrorMessage = "At least one product must be included in the deal.")]
        public List<CreateDealDetailDto> Items { get; set; } = new List<CreateDealDetailDto>();
    }
}
