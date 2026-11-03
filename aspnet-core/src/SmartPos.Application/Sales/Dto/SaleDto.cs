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

        public decimal DeliveryCharges { get; set; }

        public string DeliveryAreaName { get; set; }

        public decimal TotalAmount { get; set; }

        public int PaymentType { get; set; }

        public decimal CashAmount { get; set; }

        public decimal CardAmount { get; set; }

        public decimal CreditAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal DueAmount { get; set; }

        public string Notes { get; set; }

        public string SaleType { get; set; }

        /// <summary>True when at least one sale return exists for this sale.</summary>
        public bool HasReturns { get; set; }

        /// <summary>Number of sale return documents linked to this sale.</summary>
        public int ReturnCount { get; set; }

        /// <summary>Cash/card collections for this sale (initial + later CollectPayment).</summary>
        public List<SalePaymentHistoryItemDto> PaymentHistory { get; set; }

        public List<SaleLineDto> Lines { get; set; }
    }

    public class SalePaymentHistoryItemDto
    {
        public DateTime PaymentDate { get; set; }

        public decimal Amount { get; set; }

        /// <summary>Cash or Card.</summary>
        public string Method { get; set; }

        public string Description { get; set; }

        /// <summary>True when this row came from CollectPayment (not the original sale posting).</summary>
        public bool IsAdditionalCollection { get; set; }
    }
}
