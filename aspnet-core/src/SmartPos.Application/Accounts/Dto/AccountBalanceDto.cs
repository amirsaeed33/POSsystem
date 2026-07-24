namespace SmartPos.Accounts.Dto
{
    public class AccountBalanceDto
    {
        public int AccountId { get; set; }

        public string AccountName { get; set; }

        public string AccountType { get; set; }

        public decimal Balance { get; set; }
    }
}
