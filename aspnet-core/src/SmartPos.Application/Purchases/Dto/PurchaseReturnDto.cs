using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace SmartPos.Purchases.Dto
{
    public class PurchaseReturnDto : EntityDto
    {
        public int BranchId { get; set; }

        public string BranchName { get; set; }

        public int PurchaseId { get; set; }

        public string PurchaseInvoiceNo { get; set; }

        public string SupplierName { get; set; }

        public DateTime ReturnDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string Notes { get; set; }

        public List<PurchaseReturnLineDto> Lines { get; set; }
    }

    public class PurchaseReturnLineDto : EntityDto
    {
        public int PurchaseReturnId { get; set; }

        public int PurchaseLineId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitCost { get; set; }

        public decimal LineTotal { get; set; }
    }

    public class PurchaseReturnableDto : EntityDto
    {
        public int SupplierId { get; set; }

        public string SupplierName { get; set; }

        public string InvoiceNo { get; set; }

        public DateTime PurchaseDate { get; set; }

        public List<PurchaseReturnableLineDto> Lines { get; set; }
    }

    public class PurchaseReturnableLineDto : EntityDto
    {
        public int PurchaseLineId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal PurchasedQuantity { get; set; }

        public decimal ReturnedQuantity { get; set; }

        public decimal ReturnableQuantity { get; set; }

        public decimal UnitCost { get; set; }
    }

    public class PagedPurchaseReturnResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? PurchaseId { get; set; }
    }
}
