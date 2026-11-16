using Abp.Events.Bus;

namespace SmartPos.Orders
{
    public class CustomerOrderCreatedEventData : EventData
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; }
        public int BranchId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }

        public CustomerOrderCreatedEventData(int orderId, string orderNo, int branchId, string customerName, decimal totalAmount)
        {
            OrderId = orderId;
            OrderNo = orderNo;
            BranchId = branchId;
            CustomerName = customerName;
            TotalAmount = totalAmount;
        }
    }

    public class CustomerOrderStatusChangedEventData : EventData
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; }
        public int BranchId { get; set; }
        public CustomerOrderStatus NewStatus { get; set; }

        public CustomerOrderStatusChangedEventData(int orderId, string orderNo, int branchId, CustomerOrderStatus newStatus)
        {
            OrderId = orderId;
            OrderNo = orderNo;
            BranchId = branchId;
            NewStatus = newStatus;
        }
    }

    /// <summary>
    /// Raised after a customer/store chat message is persisted so SignalR can push it live.
    /// </summary>
    public class CustomerChatMessageSentEventData : EventData
    {
        public long Id { get; set; }
        public int BranchId { get; set; }
        public int? OrderId { get; set; }
        public string OrderNo { get; set; }
        public string SessionId { get; set; }
        public string SenderName { get; set; }
        public string CustomerMobile { get; set; }
        public ChatMessageSenderType SenderType { get; set; }
        public string Message { get; set; }
        public bool IsReadByCustomer { get; set; }
        public bool IsReadByStore { get; set; }
        public System.DateTime CreationTime { get; set; }
    }
}
