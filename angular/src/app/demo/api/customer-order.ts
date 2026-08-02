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
    branchId?: number;
    branchName?: string;
    orderDate: string | Date;
    orderNo?: string;
    status: number;
    statusName?: string;
    totalAmount: number;
    notes?: string;
    saleId?: number;
    saleInvoiceNo?: string;
    lines?: CustomerOrderLineDto[];
}

export interface CreateCustomerOrderLineDto {
    productId: number;
    quantity: number;
    unitPrice: number;
}

export interface CreateCustomerOrderDto {
    customerId: number;
    branchId: number;
    orderDate: string | Date;
    notes?: string;
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
