using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartPos.Sales;

namespace SmartPos.Sales.Dto
{
    public class CreateSaleDto
    {
        [Required]
        public int CustomerId { get; set; }

        public DateTime SaleDate { get; set; }

        [StringLength(Sale.MaxInvoiceNoLength)]
        public string InvoiceNo { get; set; }

        [StringLength(Sale.MaxNotesLength)]
        public string Notes { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [Range(0, 100)]
        public decimal TaxPercent { get; set; }

        /// <summary><see cref="PaymentTypes"/> value. Defaults to Credit.</summary>
        public int PaymentType { get; set; } = PaymentTypes.Credit;

        [Range(0, double.MaxValue)]
        public decimal CashAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CardAmount { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateSaleLineDto> Lines { get; set; }
    }

    public class CreateSaleLineDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}
