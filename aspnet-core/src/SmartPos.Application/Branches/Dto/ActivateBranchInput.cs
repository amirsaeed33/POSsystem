using System.ComponentModel.DataAnnotations;

namespace SmartPos.Branches.Dto
{
    public class ActivateBranchInput
    {
        [Required]
        public string Token { get; set; }
    }
}
