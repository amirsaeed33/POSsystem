using System.ComponentModel.DataAnnotations;

namespace SmartPos.Deals.Dto
{
    public class UpdateDealDto : CreateDealDto
    {
        [Required]
        public int Id { get; set; }
    }
}
