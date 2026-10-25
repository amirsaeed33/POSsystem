export enum ChatMessageSenderType {
    Customer = 0,
    Store = 1,
    System = 2
}

export interface CustomerChatMessageDto {
    id: number;
    branchId: number;
    orderId?: number;
    orderNo?: string;
    sessionId?: string;
    senderName: string;
    customerMobile?: string;
    senderType: ChatMessageSenderType;
    message: string;
    isReadByCustomer: boolean;
    isReadByStore: boolean;
    creationTime: string | Date;
}

export interface SendCustomerChatMessageInput {
    branchId?: number;
    orderId?: number;
    orderNo?: string;
    sessionId?: string;
    senderName?: string;
    customerMobile?: string;
    senderType: ChatMessageSenderType;
    message: string;
}

export interface CustomerOrderConversationDto {
    orderId?: number;
    orderNo: string;
    branchId: number;
    customerName: string;
    customerMobile?: string;
    lastMessage: string;
    lastMessageTime: string | Date;
    unreadCount: number;
    orderStatus?: string;
}
