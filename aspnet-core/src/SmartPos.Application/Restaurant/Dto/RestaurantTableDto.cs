using Abp.Application.Services.Dto;
using SmartPos.Restaurant;

namespace SmartPos.Restaurant.Dto
{
    public class RestaurantTableDto : EntityDto
    {
        public int BranchId { get; set; }

        public string TableNo { get; set; }

        public string Area { get; set; }

        public int Capacity { get; set; }

        public TableStatus Status { get; set; }

        public string StatusName => Status.ToString();

        public int? CurrentOrderId { get; set; }

        public bool IsActive { get; set; }
    }
}
