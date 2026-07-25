using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace SmartPos.Sales.Dto
{
    public class SaleDto : EntityDto
    {
        [Required]
        public int CustomerId { get; set; }

        public string CustomerName { get; set; }

        public DateTime SaleDate { get; set; }

        public string InvoiceNo { get; set; }

        public decimal SubTotal { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public int PaymentType { get; set; }

        public decimal CashAmount { get; set; }

        public decimal CardAmount { get; set; }

        public decimal CreditAmount { get; set; }

        public string Notes { get; set; }

        public List<SaleLineDto> Lines { get; set; }
    }
}
