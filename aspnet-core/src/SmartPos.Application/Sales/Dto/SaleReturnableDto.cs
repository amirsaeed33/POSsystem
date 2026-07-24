using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace SmartPos.Sales.Dto
{
    public class SaleReturnableDto : EntityDto
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string InvoiceNo { get; set; }

        public System.DateTime SaleDate { get; set; }

        public List<SaleReturnableLineDto> Lines { get; set; }
    }

    public class SaleReturnableLineDto : EntityDto
    {
        public int SaleLineId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal SoldQuantity { get; set; }

        public decimal ReturnedQuantity { get; set; }

        public decimal ReturnableQuantity { get; set; }

        public decimal UnitPrice { get; set; }
    }

    public class PagedSaleReturnResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? SaleId { get; set; }
    }
}
