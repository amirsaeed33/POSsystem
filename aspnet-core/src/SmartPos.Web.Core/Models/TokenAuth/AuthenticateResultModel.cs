namespace SmartPos.Models.TokenAuth
{
    public class AuthenticateResultModel
    {
        public string AccessToken { get; set; }

        public string EncryptedAccessToken { get; set; }

        public int ExpireInSeconds { get; set; }

        public long UserId { get; set; }

        /// <summary>
        /// Tenant of the authenticated user (null for host users). Client should store this in session/cookie.
        /// </summary>
        public int? TenantId { get; set; }

        public string TenancyName { get; set; }
    }
}
