namespace SmartPos.Authorization.Accounts.Dto
{
    public class SignUpTenantOutput
    {
        public int TenantId { get; set; }

        public string TenancyName { get; set; }

        public string Name { get; set; }

        public string AdminUserName { get; set; }

        public bool CanLogin { get; set; }
    }
}
