using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace SmartPos.Purchases.Dto
{
    public class PurchaseLineDto : EntityDto
    {
        public int PurchaseId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitCost { get; set; }

        public decimal LineTotal { get; set; }
    }
}
