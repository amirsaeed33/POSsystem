using System.ComponentModel.DataAnnotations;
using Abp.Auditing;
using Abp.Authorization.Users;

namespace SmartPos.Models.TokenAuth
{
    public class AuthenticateModel
    {
        [Required]
        [StringLength(AbpUserBase.MaxEmailAddressLength)]
        public string UserNameOrEmailAddress { get; set; }

        [Required]
        [StringLength(AbpUserBase.MaxPlainPasswordLength)]
        [DisableAuditing]
        public string Password { get; set; }

        public bool RememberClient { get; set; }

        /// <summary>Optional client GPS latitude (waiter/rider auto attendance check-in).</summary>
        public double? Latitude { get; set; }

        /// <summary>Optional client GPS longitude (waiter/rider auto attendance check-in).</summary>
        public double? Longitude { get; set; }
    }
}
