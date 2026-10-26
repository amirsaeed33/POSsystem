using System.Threading.Tasks;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.Dependency;
using SmartPos.Orders;
using SmartPos.Orders.Dto;

namespace SmartPos.Chat
{
    /// <summary>
    /// Real-time order/session chat hub. Persistence goes through <see cref="ICustomerChatAppService"/>
    /// so UnitOfWork + SignalR broadcast (via domain event) stay consistent for hub and HTTP paths.
    /// </summary>
    public class OrderChatHub : AbpHubBase, ITransientDependency
    {
        private readonly ICustomerChatAppService _customerChatAppService;

        public OrderChatHub(ICustomerChatAppService customerChatAppService)
        {
            _customerChatAppService = customerChatAppService;
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
            return await _customerChatAppService.SendMessageAsync(input);
        }
    }
}
