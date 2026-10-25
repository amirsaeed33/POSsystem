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
}
