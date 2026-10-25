using Abp.Application.Services.Dto;

namespace SmartPos.Deals.Dto
{
    public class DealDetailDto : EntityDto
    {
        public int DealId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductBarcode { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal SubTotal => Quantity * UnitPrice;
    }
}
