export enum PaymentType {
    Cash = 0,
    Card = 1,
    Credit = 2,
    Mixed = 3,
}

export interface SaleLineDto {
    id: number;
    saleId: number;
    productId: number;
    productName?: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
}

export interface SaleDto {
    id: number;
    customerId: number;
    customerName?: string;
    saleDate: string | Date;
    invoiceNo?: string;
    subTotal: number;
    discountAmount: number;
    discountPercent: number;
    taxPercent: number;
    taxAmount: number;
    totalAmount: number;
    paymentType: number;
    cashAmount: number;
    cardAmount: number;
    creditAmount: number;
    notes?: string;
    hasReturns?: boolean;
    returnCount?: number;
    lines?: SaleLineDto[];
}

export interface CreateSaleLineDto {
    productId: number;
    quantity: number;
    unitPrice: number;
}

export interface CreateSaleDto {
    customerId: number;
    saleDate: string | Date;
    invoiceNo?: string;
    notes?: string;
    discountAmount: number;
    discountPercent: number;
    taxPercent: number;
    paymentType: number;
    cashAmount: number;
    cardAmount: number;
    lines: CreateSaleLineDto[];
}

export interface PagedSaleResultRequestDto {
    keyword?: string;
    customerId?: number;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
