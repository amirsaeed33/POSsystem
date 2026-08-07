using System.ComponentModel.DataAnnotations;

namespace SmartPos.Staffs.Dto
{
    public class CreateStaffLoginDto
    {
        [Required]
        public int StaffId { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(Staff.MaxEmailLength)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }
    }
}
