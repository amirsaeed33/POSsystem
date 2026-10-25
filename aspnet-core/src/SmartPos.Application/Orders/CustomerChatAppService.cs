using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Orders.Dto;

namespace SmartPos.Orders
{
    public class CustomerChatAppService : ApplicationService, ICustomerChatAppService
    {
        private readonly IRepository<CustomerChatMessage, long> _chatMessageRepository;
        private readonly IRepository<CustomerOrder> _orderRepository;

        public CustomerChatAppService(
            IRepository<CustomerChatMessage, long> chatMessageRepository,
            IRepository<CustomerOrder> orderRepository)
        {
            _chatMessageRepository = chatMessageRepository;
            _orderRepository = orderRepository;
        }

        public async Task<List<CustomerChatMessageDto>> GetMessagesByOrderNoAsync(string orderNo)
        {
            if (string.IsNullOrWhiteSpace(orderNo))
            {
                return new List<CustomerChatMessageDto>();
            }

            var cleanOrderNo = orderNo.Trim().ToUpperInvariant();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var messages = await _chatMessageRepository.GetAll()
                    .Where(x => x.OrderNo == cleanOrderNo)
                    .OrderBy(x => x.CreationTime)
                    .Take(100)
                    .Select(x => new CustomerChatMessageDto
                    {
                        Id = x.Id,
                        BranchId = x.BranchId,
                        OrderId = x.OrderId,
                        OrderNo = x.OrderNo,
                        SessionId = x.SessionId,
                        SenderName = x.SenderName,
                        CustomerMobile = x.CustomerMobile,
                        SenderType = x.SenderType,
                        Message = x.Message,
                        IsReadByCustomer = x.IsReadByCustomer,
                        IsReadByStore = x.IsReadByStore,
                        CreationTime = x.CreationTime
                    })
                    .ToListAsync();

                return messages;
            }
        }

        public async Task<List<CustomerChatMessageDto>> GetMessagesBySessionAsync(string sessionId, int branchId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return new List<CustomerChatMessageDto>();
            }

            var cleanSessionId = sessionId.Trim();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var query = _chatMessageRepository.GetAll()
                    .Where(x => x.SessionId == cleanSessionId);

                if (branchId > 0)
                {
                    query = query.Where(x => x.BranchId == branchId);
                }

                var messages = await query
                    .OrderBy(x => x.CreationTime)
                    .Take(100)
                    .Select(x => new CustomerChatMessageDto
                    {
                        Id = x.Id,
                        BranchId = x.BranchId,
                        OrderId = x.OrderId,
                        OrderNo = x.OrderNo,
                        SessionId = x.SessionId,
                        SenderName = x.SenderName,
                        CustomerMobile = x.CustomerMobile,
                        SenderType = x.SenderType,
                        Message = x.Message,
                        IsReadByCustomer = x.IsReadByCustomer,
                        IsReadByStore = x.IsReadByStore,
                        CreationTime = x.CreationTime
                    })
                    .ToListAsync();

                return messages;
            }
        }

        public async Task<CustomerChatMessageDto> SendMessageAsync(SendCustomerChatMessageInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Message))
            {
                throw new UserFriendlyException("Message cannot be empty.");
            }

            var cleanOrderNo = input.OrderNo?.Trim()?.ToUpperInvariant();
            var cleanSessionId = input.SessionId?.Trim();

            int? resolvedOrderId = input.OrderId;
            if (!resolvedOrderId.HasValue && !string.IsNullOrWhiteSpace(cleanOrderNo))
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
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

            await _chatMessageRepository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();

            return new CustomerChatMessageDto
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
        }

        public async Task<List<CustomerOrderConversationDto>> GetBranchConversationsAsync(int branchId)
        {
            var query = _chatMessageRepository.GetAll();
            if (branchId > 0)
            {
                query = query.Where(x => x.BranchId == branchId);
            }

            // Group by OrderNo or SessionId
            var latestMessages = await query
                .OrderByDescending(x => x.CreationTime)
                .Take(200)
                .ToListAsync();

            var groups = latestMessages
                .GroupBy(x => !string.IsNullOrWhiteSpace(x.OrderNo) ? "ORD_" + x.OrderNo : "SES_" + x.SessionId)
                .Select(g =>
                {
                    var first = g.First();
                    var unread = g.Count(x => !x.IsReadByStore && x.SenderType == ChatMessageSenderType.Customer);
                    return new CustomerOrderConversationDto
                    {
                        OrderNo = first.OrderNo,
                        SessionId = first.SessionId,
                        CustomerName = first.SenderName,
                        CustomerMobile = first.CustomerMobile,
                        LastMessage = first.Message,
                        LastMessageTime = first.CreationTime,
                        UnreadCount = unread,
                        BranchId = first.BranchId,
                        OrderId = first.OrderId
                    };
                })
                .OrderByDescending(x => x.LastMessageTime)
                .ToList();

            return groups;
        }

        public async Task<int> GetBranchUnreadCountAsync(int branchId)
        {
            var query = _chatMessageRepository.GetAll()
                .Where(x => !x.IsReadByStore && x.SenderType == ChatMessageSenderType.Customer);

            if (branchId > 0)
            {
                query = query.Where(x => x.BranchId == branchId);
            }

            return await query.CountAsync();
        }

        public async Task MarkOrderMessagesAsReadAsync(string orderNo, bool isCustomer)
        {
            if (string.IsNullOrWhiteSpace(orderNo)) return;
            var clean = orderNo.Trim().ToUpperInvariant();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var messages = await _chatMessageRepository.GetAll()
                    .Where(x => x.OrderNo == clean && (isCustomer ? !x.IsReadByCustomer : !x.IsReadByStore))
                    .ToListAsync();

                foreach (var msg in messages)
                {
                    if (isCustomer) msg.IsReadByCustomer = true;
                    else msg.IsReadByStore = true;
                }

                await CurrentUnitOfWork.SaveChangesAsync();
            }
        }

        public async Task MarkSessionMessagesAsReadAsync(string sessionId, int branchId, bool isCustomer)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return;
            var clean = sessionId.Trim();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var query = _chatMessageRepository.GetAll()
                    .Where(x => x.SessionId == clean && (isCustomer ? !x.IsReadByCustomer : !x.IsReadByStore));

                if (branchId > 0)
                {
                    query = query.Where(x => x.BranchId == branchId);
                }

                var messages = await query.ToListAsync();

                foreach (var msg in messages)
                {
                    if (isCustomer) msg.IsReadByCustomer = true;
                    else msg.IsReadByStore = true;
                }

                await CurrentUnitOfWork.SaveChangesAsync();
            }
        }
    }
}
