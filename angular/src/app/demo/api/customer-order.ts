export enum CustomerOrderStatus {
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

export interface CustomerOrderLineDto {
    id: number;
    orderId: number;
    productId: number;
    productName?: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
}

export interface CustomerOrderDto {
    id: number;
    customerId: number;
    customerName?: string;
    orderDate: string | Date;
    orderNo?: string;
    status: number;
    statusName?: string;
    subTotal?: number;
    deliveryFee?: number;
    deliveryAreaId?: number;
    deliveryAreaName?: string;
    isFreeDelivery?: boolean;
    freeDeliveryReason?: string;
    originalDeliveryFee?: number;
    deliveryFeeOverrideReason?: string;
    deliveryFeeOverriddenByName?: string;
    deliveryFeeOverrideTime?: string | Date;
    totalAmount: number;
    notes?: string;
    saleId?: number;
    saleInvoiceNo?: string;
    lines?: CustomerOrderLineDto[];
}

export interface OverrideDeliveryFeeInput {
    orderId: number;
    newDeliveryFee: number;
    reason: string;
}

export interface CreateCustomerOrderLineDto {
    productId: number;
    quantity: number;
    unitPrice: number;
}

export interface CreateCustomerOrderDto {
    customerId?: number;
    customerName?: string;
    customerMobile?: string;
    branchId?: number;
    orderDate?: string | Date;
    notes?: string;
    latitude?: number;
    longitude?: number;
    googleMapsLink?: string;
    google_maps_link?: string;
    deliveryAddress?: string;
    deliveryLandmark?: string;
    deliveryAreaId?: number;
    deliveryArea?: string;
    subtotal?: number;
    deliveryCharges?: number;
    grandTotal?: number;
    lines: CreateCustomerOrderLineDto[];
}

export interface PagedCustomerOrderResultRequestDto {
    keyword?: string;
    status?: number;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
