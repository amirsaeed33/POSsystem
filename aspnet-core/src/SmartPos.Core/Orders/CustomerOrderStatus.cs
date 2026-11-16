namespace SmartPos.Orders
{
    public enum CustomerOrderStatus : byte
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        InProgress = 3,
        /// <summary>Handed to customer by rider (or admin). Payment / sale is recorded on Completed.</summary>
        Delivered = 4,
        /// <summary>Admin confirmed payment and posted the sale into the system.</summary>
        Completed = 5
    }
}
