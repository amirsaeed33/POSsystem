namespace SmartPos.Customers
{
    public static class CustomerTypes
    {
        public const int Direct = 0;
        public const int Wholesaler = 1;

        public static bool IsWholesaler(int customerType)
        {
            return customerType == Wholesaler;
        }
    }
}
