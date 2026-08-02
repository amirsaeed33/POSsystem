using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartPos.Purchases;

namespace SmartPos.Purchases.Dto
{
    public class CreatePurchaseReturnDto
    {
        [Required]
        public int BranchId { get; set; }

        [Required]
        public int PurchaseId { get; set; }

        public DateTime ReturnDate { get; set; }

        [StringLength(PurchaseReturn.MaxNotesLength)]
        public string Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreatePurchaseReturnLineDto> Lines { get; set; }
    }

    public class CreatePurchaseReturnLineDto
    {
        [Required]
        public int PurchaseLineId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }
    }
}
