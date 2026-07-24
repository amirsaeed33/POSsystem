using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace SmartPos.Sales.Dto
{
    public class SaleLineDto : EntityDto
    {
        public int SaleId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}
