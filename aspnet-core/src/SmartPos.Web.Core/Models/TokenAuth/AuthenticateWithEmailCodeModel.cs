using System.ComponentModel.DataAnnotations;
using Abp.Auditing;
using Abp.Authorization.Users;

namespace SmartPos.Models.TokenAuth
{
    public class AuthenticateWithEmailCodeModel
    {
        [Required]
        [EmailAddress]
        [StringLength(AbpUserBase.MaxEmailAddressLength)]
        public string EmailAddress { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        [DisableAuditing]
        public string Code { get; set; }
    }
}
