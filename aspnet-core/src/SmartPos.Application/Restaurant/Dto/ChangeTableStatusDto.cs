using System.ComponentModel.DataAnnotations;
using SmartPos.Restaurant;

namespace SmartPos.Restaurant.Dto
{
    public class ChangeTableStatusDto
    {
        [Required]
        public int TableId { get; set; }

        [Required]
        public TableStatus Status { get; set; }

        public int? CurrentOrderId { get; set; }
    }
}
