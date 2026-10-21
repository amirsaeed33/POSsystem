using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Orders;

namespace SmartPos.Orders.Dto
{
    public class DeliveryAreaDto : EntityDto
    {
        public int BranchId { get; set; }

        public string BranchName { get; set; }

        [Required]
        [StringLength(DeliveryArea.MaxNameLength)]
        public string Name { get; set; }

        public decimal DeliveryFee { get; set; }

        public decimal MinimumOrderAmount { get; set; }

        [StringLength(DeliveryArea.MaxEstimatedTimeLength)]
        public string EstimatedDeliveryTime { get; set; }

        [StringLength(DeliveryArea.MaxCoverageDetailsLength)]
        public string CoverageDetails { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreateDeliveryAreaDto
    {
        public int? BranchId { get; set; }

        [Required]
        [StringLength(DeliveryArea.MaxNameLength)]
        public string Name { get; set; }

        [Range(0, 100000)]
        public decimal DeliveryFee { get; set; }

        [Range(0, 1000000)]
        public decimal MinimumOrderAmount { get; set; }

        [StringLength(DeliveryArea.MaxEstimatedTimeLength)]
        public string EstimatedDeliveryTime { get; set; }

        [StringLength(DeliveryArea.MaxCoverageDetailsLength)]
        public string CoverageDetails { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class PagedDeliveryAreaResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? BranchId { get; set; }

        public bool? IsActive { get; set; }
    }
}
