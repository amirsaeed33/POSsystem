namespace SmartPos.Models.TokenAuth
{
    public class SendEmailLoginCodeResultModel
    {
        public int ExpirationMinutes { get; set; }

        public int ResendCooldownSeconds { get; set; }
    }
}
