using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace SmartPos.Sales.Dto
{
    public class SaleReturnDto : EntityDto
    {
        public int BranchId { get; set; }

        public string BranchName { get; set; }

        public int SaleId { get; set; }

        public string SaleInvoiceNo { get; set; }

        public string CustomerName { get; set; }

        public DateTime ReturnDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string Notes { get; set; }

        public List<SaleReturnLineDto> Lines { get; set; }
    }

    public class SaleReturnLineDto : EntityDto
    {
        public int SaleReturnId { get; set; }

        public int SaleLineId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}
