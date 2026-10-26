using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Orders;

namespace SmartPos.Orders.Dto
{
    public class CustomerChatMessageDto : EntityDto<long>
    {
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

        public DateTime CreationTime { get; set; }
    }

    public class SendCustomerChatMessageInput
    {
        public int BranchId { get; set; }

        public int? OrderId { get; set; }

        [StringLength(CustomerChatMessage.MaxOrderNoLength)]
        public string OrderNo { get; set; }

        [StringLength(CustomerChatMessage.MaxSessionIdLength)]
        public string SessionId { get; set; }

        [StringLength(CustomerChatMessage.MaxSenderNameLength)]
        public string SenderName { get; set; }

        [StringLength(CustomerChatMessage.MaxMobileLength)]
        public string CustomerMobile { get; set; }

        public ChatMessageSenderType SenderType { get; set; }

        [Required]
        [StringLength(CustomerChatMessage.MaxMessageLength)]
        public string Message { get; set; }
    }

    public class GetOrderChatInput
    {
        public string OrderNo { get; set; }

        public string SessionId { get; set; }

        public int? BranchId { get; set; }
    }

    public class CustomerOrderConversationDto
    {
        public string OrderNo { get; set; }

        public string SessionId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerMobile { get; set; }

        public string LastMessage { get; set; }

        public DateTime LastMessageTime { get; set; }

        public int UnreadCount { get; set; }

        public int BranchId { get; set; }

        public int? OrderId { get; set; }

        public CustomerOrderStatus? OrderStatus { get; set; }
    }
}
