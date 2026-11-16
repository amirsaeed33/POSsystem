using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Events.Bus.Handlers;
using Microsoft.AspNetCore.SignalR;
using SmartPos.Orders;

namespace SmartPos.Chat
{
    public class CustomerOrderSignalRNotifier :
        IAsyncEventHandler<CustomerOrderCreatedEventData>,
        IAsyncEventHandler<CustomerOrderStatusChangedEventData>,
        ITransientDependency
    {
        private readonly IHubContext<OrderChatHub> _hubContext;

        public CustomerOrderSignalRNotifier(IHubContext<OrderChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task HandleEventAsync(CustomerOrderCreatedEventData eventData)
        {
            if (eventData.BranchId > 0)
            {
                await _hubContext.Clients.Group("branch_" + eventData.BranchId).SendAsync("NewOrderReceived", new
                {
                    orderId = eventData.OrderId,
                    orderNo = eventData.OrderNo,
                    branchId = eventData.BranchId,
                    customerName = eventData.CustomerName,
                    totalAmount = eventData.TotalAmount
                });
            }
        }

        public async Task HandleEventAsync(CustomerOrderStatusChangedEventData eventData)
        {
            var payload = new
            {
                orderId = eventData.OrderId,
                orderNo = eventData.OrderNo,
                branchId = eventData.BranchId,
                status = (int)eventData.NewStatus
            };

            if (eventData.BranchId > 0)
            {
                await _hubContext.Clients.Group("branch_" + eventData.BranchId).SendAsync("OrderStatusChanged", payload);
            }

            if (!string.IsNullOrWhiteSpace(eventData.OrderNo))
            {
                await _hubContext.Clients.Group("order_" + eventData.OrderNo.Trim().ToUpperInvariant()).SendAsync("OrderStatusChanged", payload);
            }
        }
    }
}
