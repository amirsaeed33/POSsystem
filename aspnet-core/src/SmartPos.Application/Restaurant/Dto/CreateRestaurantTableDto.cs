using System.ComponentModel.DataAnnotations;
using SmartPos.Restaurant;

namespace SmartPos.Restaurant.Dto
{
    public class CreateRestaurantTableDto
    {
        [Required]
        public int BranchId { get; set; }

        [Required]
        [StringLength(RestaurantTable.MaxTableNoLength)]
        public string TableNo { get; set; }

        [StringLength(RestaurantTable.MaxAreaLength)]
        public string Area { get; set; } = "Main Hall";

        public int Capacity { get; set; } = 4;

        public bool IsCarBay { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
