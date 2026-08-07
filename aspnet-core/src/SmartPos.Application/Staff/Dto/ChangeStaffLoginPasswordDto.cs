using System.ComponentModel.DataAnnotations;

namespace SmartPos.Staffs.Dto
{
    public class ChangeStaffLoginPasswordDto
    {
        [Required]
        public int StaffId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }
}
