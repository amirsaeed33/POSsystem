using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartPos.Purchases;

namespace SmartPos.Purchases.Dto
{
    public class CreatePurchaseDto
    {
        [Required]
        public int BranchId { get; set; }

        [Required]
        public int SupplierId { get; set; }

        public DateTime PurchaseDate { get; set; }

        [StringLength(Purchase.MaxInvoiceNoLength)]
        public string InvoiceNo { get; set; }

        [StringLength(Purchase.MaxNotesLength)]
        public string Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreatePurchaseLineDto> Lines { get; set; }
    }

    public class CreatePurchaseLineDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitCost { get; set; }
    }
}
