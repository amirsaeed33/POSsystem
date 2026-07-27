using System.ComponentModel.DataAnnotations;
using Abp.Authorization.Users;

namespace SmartPos.Models.TokenAuth
{
    public class ExternalAuthenticateModel
    {
        [Required]
        [StringLength(AbpUserBase.MaxUserNameLength)]
        public string AuthProvider { get; set; }

        [MaxLength(AbpUserBase.MaxEmailAddressLength * 4)]
        public string ProviderKey { get; set; }

        [Required]
        public string ProviderAccessCode { get; set; }
    }
}
