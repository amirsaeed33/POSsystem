using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.Orders.Dto;

namespace SmartPos.Orders
{
    public interface ICustomerChatAppService : IApplicationService
    {
        Task<List<CustomerChatMessageDto>> GetMessagesByOrderNoAsync(string orderNo);

        Task<List<CustomerChatMessageDto>> GetMessagesBySessionAsync(string sessionId, int branchId);

        Task<CustomerChatMessageDto> SendMessageAsync(SendCustomerChatMessageInput input);

        Task<List<CustomerOrderConversationDto>> GetBranchConversationsAsync(int branchId);

        Task<int> GetBranchUnreadCountAsync(int branchId);

        Task MarkOrderMessagesAsReadAsync(string orderNo, bool isCustomer);

        Task MarkSessionMessagesAsReadAsync(string sessionId, int branchId, bool isCustomer);
    }
}
