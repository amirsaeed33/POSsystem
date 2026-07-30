using System.ComponentModel.DataAnnotations;
using Abp.Authorization.Users;

namespace SmartPos.Models.TokenAuth
{
    public class SendEmailLoginCodeModel
    {
        [Required]
        [EmailAddress]
        [StringLength(AbpUserBase.MaxEmailAddressLength)]
        public string EmailAddress { get; set; }
    }
}
