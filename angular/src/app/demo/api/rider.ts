export enum RiderOrderStatus {
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    InProgress = 3,
    Delivered = 4
}

export interface RiderDashboardStatsDto {
    todayDeliveredCount: number;
    activeAssignedCount: number;
    todayDeliveredAmount: number;
    activePendingAmount: number;
    totalAssignedToday: number;
    riderStaffId: number;
    riderName: string;
    riderPhone?: string;
    branchName?: string;
}

export interface RiderOrderItemDto {
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
    isDeal: boolean;
    dealItemDescriptions?: string[];
}

export interface RiderOrderDto {
    id: number;
    orderNo: string;
    orderDate: string;
    status: RiderOrderStatus;
    statusText: string;
    riderAssignedTime?: string;
    riderAssignedTimeText?: string;
    customerId: number;
    customerName: string;
    customerPhone?: string;
    customerAddress?: string;
    deliveryAreaName?: string;
    latitude?: number | null;
    longitude?: number | null;
    distanceKm?: number | null;
    subTotal: number;
    deliveryFee: number;
    totalAmount: number;
    notes?: string;
    paymentMethod: string;
    isAcceptedByRider: boolean;
    items: RiderOrderItemDto[];
}

export interface RiderOrderHistoryInput {
    fromDate?: string;
    toDate?: string;
    keyword?: string;
    status?: RiderOrderStatus;
    skipCount?: number;
    maxResultCount?: number;
}

export interface RejectRiderOrderDto {
    orderId: number;
    reason?: string;
}

export interface PagedRiderOrderResultDto {
    totalCount: number;
    items: RiderOrderDto[];
}
