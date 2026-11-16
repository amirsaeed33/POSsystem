using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Events.Bus.Handlers;
using Microsoft.AspNetCore.SignalR;
using SmartPos.Orders;
using SmartPos.Orders.Dto;

namespace SmartPos.Chat
{
    public class CustomerChatSignalRNotifier :
        IAsyncEventHandler<CustomerChatMessageSentEventData>,
        ITransientDependency
    {
        private readonly IHubContext<OrderChatHub> _hubContext;

        public CustomerChatSignalRNotifier(IHubContext<OrderChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task HandleEventAsync(CustomerChatMessageSentEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            var dto = new CustomerChatMessageDto
            {
                Id = eventData.Id,
                BranchId = eventData.BranchId,
                OrderId = eventData.OrderId,
                OrderNo = eventData.OrderNo,
                SessionId = eventData.SessionId,
                SenderName = eventData.SenderName,
                CustomerMobile = eventData.CustomerMobile,
                SenderType = eventData.SenderType,
                Message = eventData.Message,
                IsReadByCustomer = eventData.IsReadByCustomer,
                IsReadByStore = eventData.IsReadByStore,
                CreationTime = eventData.CreationTime
            };

            if (!string.IsNullOrWhiteSpace(dto.OrderNo))
            {
                await _hubContext.Clients
                    .Group("order_" + dto.OrderNo.Trim().ToUpperInvariant())
                    .SendAsync("ReceiveMessage", dto);
            }

            if (!string.IsNullOrWhiteSpace(dto.SessionId))
            {
                await _hubContext.Clients
                    .Group("session_" + dto.SessionId.Trim())
                    .SendAsync("ReceiveMessage", dto);
            }

            if (dto.BranchId > 0)
            {
                await _hubContext.Clients
                    .Group("branch_" + dto.BranchId)
                    .SendAsync("ReceiveMessage", dto);

                if (dto.SenderType == ChatMessageSenderType.Customer)
                {
                    await _hubContext.Clients
                        .Group("branch_" + dto.BranchId)
                        .SendAsync("OrderMessageNotification", dto);
                }
            }
        }
    }
}
