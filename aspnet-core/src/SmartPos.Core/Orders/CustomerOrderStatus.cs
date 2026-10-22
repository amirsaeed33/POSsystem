namespace SmartPos.Orders
{
    public enum CustomerOrderStatus : byte
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        InProgress = 3,
        Delivered = 4
    }
}
