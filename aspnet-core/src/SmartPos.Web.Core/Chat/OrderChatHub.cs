using System;
using System.Threading.Tasks;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.SignalR;
using SmartPos.Orders;
using SmartPos.Orders.Dto;

namespace SmartPos.Chat
{
    public class OrderChatHub : AbpHubBase, ITransientDependency
    {
        private readonly IRepository<CustomerChatMessage, long> _chatMessageRepository;
        private readonly IRepository<CustomerOrder> _orderRepository;

        public OrderChatHub(
            IRepository<CustomerChatMessage, long> chatMessageRepository,
            IRepository<CustomerOrder> orderRepository)
        {
            _chatMessageRepository = chatMessageRepository;
            _orderRepository = orderRepository;
        }

        public async Task JoinOrderChat(string orderNo)
        {
            if (!string.IsNullOrWhiteSpace(orderNo))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "order_" + orderNo.Trim().ToUpperInvariant());
            }
        }

        public async Task LeaveOrderChat(string orderNo)
        {
            if (!string.IsNullOrWhiteSpace(orderNo))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "order_" + orderNo.Trim().ToUpperInvariant());
            }
        }

        public async Task JoinSessionChat(string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "session_" + sessionId.Trim());
            }
        }

        public async Task JoinBranchChat(int branchId)
        {
            if (branchId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "branch_" + branchId);
            }
        }

        public async Task LeaveBranchChat(int branchId)
        {
            if (branchId > 0)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "branch_" + branchId);
            }
        }

        public async Task<CustomerChatMessageDto> SendMessage(SendCustomerChatMessageInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Message))
            {
                throw new ArgumentException("Message cannot be empty.");
            }

            var cleanOrderNo = input.OrderNo?.Trim()?.ToUpperInvariant();
            var cleanSessionId = input.SessionId?.Trim();

            int? resolvedOrderId = input.OrderId;
            if (!resolvedOrderId.HasValue && !string.IsNullOrWhiteSpace(cleanOrderNo))
            {
                var order = await _orderRepository.FirstOrDefaultAsync(x => x.OrderNo == cleanOrderNo);
                if (order != null)
                {
                    resolvedOrderId = order.Id;
                    if (input.BranchId <= 0)
                    {
                        input.BranchId = order.BranchId;
                    }
                }
            }

            var entity = new CustomerChatMessage
            {
                TenantId = AbpSession.TenantId,
                BranchId = input.BranchId,
                OrderId = resolvedOrderId,
                OrderNo = cleanOrderNo,
                SessionId = cleanSessionId,
                SenderName = string.IsNullOrWhiteSpace(input.SenderName)
                    ? (input.SenderType == ChatMessageSenderType.Customer ? "Customer" : "Store Staff")
                    : input.SenderName.Trim(),
                CustomerMobile = input.CustomerMobile?.Trim(),
                SenderType = input.SenderType,
                Message = input.Message.Trim(),
                IsReadByCustomer = input.SenderType == ChatMessageSenderType.Customer,
                IsReadByStore = input.SenderType == ChatMessageSenderType.Store,
                CreationTime = DateTime.UtcNow,
                CreatorUserId = AbpSession.UserId
            };

            entity.Id = await _chatMessageRepository.InsertAndGetIdAsync(entity);

            var dto = new CustomerChatMessageDto
            {
                Id = entity.Id,
                BranchId = entity.BranchId,
                OrderId = entity.OrderId,
                OrderNo = entity.OrderNo,
                SessionId = entity.SessionId,
                SenderName = entity.SenderName,
                CustomerMobile = entity.CustomerMobile,
                SenderType = entity.SenderType,
                Message = entity.Message,
                IsReadByCustomer = entity.IsReadByCustomer,
                IsReadByStore = entity.IsReadByStore,
                CreationTime = entity.CreationTime
            };

            // Broadcast to order group
            if (!string.IsNullOrWhiteSpace(cleanOrderNo))
            {
                await Clients.Group("order_" + cleanOrderNo).SendAsync("ReceiveMessage", dto);
            }

            // Broadcast to session group (for pre-order inquiries)
            if (!string.IsNullOrWhiteSpace(cleanSessionId))
            {
                await Clients.Group("session_" + cleanSessionId).SendAsync("ReceiveMessage", dto);
            }

            // Broadcast to branch store staff
            if (entity.BranchId > 0)
            {
                await Clients.Group("branch_" + entity.BranchId).SendAsync("ReceiveMessage", dto);
                if (input.SenderType == ChatMessageSenderType.Customer)
                {
                    await Clients.Group("branch_" + entity.BranchId).SendAsync("OrderMessageNotification", dto);
                }
            }

            return dto;
        }
    }
}
