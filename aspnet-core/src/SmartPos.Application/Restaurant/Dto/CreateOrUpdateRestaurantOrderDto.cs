using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartPos.Restaurant;

namespace SmartPos.Restaurant.Dto
{
    public class CreateOrUpdateRestaurantOrderDto
    {
        public int? Id { get; set; }

        [Required]
        public int BranchId { get; set; }

        public int? TableId { get; set; }

        public string TableNo { get; set; }

        public OrderType OrderType { get; set; } = OrderType.DineIn;

        public string CarName { get; set; }

        public string CarNumber { get; set; }

        public string CarLocation { get; set; }

        public int? CustomerId { get; set; }

        public string WaiterName { get; set; }

        public string Notes { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal ServiceChargePercent { get; set; }

        public List<CreateOrUpdateRestaurantOrderLineDto> Lines { get; set; } = new List<CreateOrUpdateRestaurantOrderLineDto>();
    }

    public class CreateOrUpdateRestaurantOrderLineDto
    {
        public int? Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public string KitchenNotes { get; set; }

        public string SelectedModifiers { get; set; }

        public bool IsSentToKitchen { get; set; }

        public decimal PrintedQuantity { get; set; } = 0;
    }
}
