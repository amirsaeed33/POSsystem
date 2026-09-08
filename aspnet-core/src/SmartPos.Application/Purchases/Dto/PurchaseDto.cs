using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace SmartPos.Purchases.Dto
{
    public class PurchaseDto : EntityDto
    {
        [Required]
        public int SupplierId { get; set; }

        public string SupplierName { get; set; }

        public DateTime PurchaseDate { get; set; }

        public string InvoiceNo { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal AmountPaid { get; set; }

        public string PaymentStatus { get; set; }

        public decimal DueAmount { get; set; }

        public string Notes { get; set; }

        public List<PurchaseLineDto> Lines { get; set; }
    }
}
