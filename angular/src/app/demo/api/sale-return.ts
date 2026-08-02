export interface SaleReturnLineDto {
    id: number;
    saleReturnId: number;
    saleLineId: number;
    productId: number;
    productName?: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
}

export interface SaleReturnDto {
    id: number;
    saleId: number;
    saleInvoiceNo?: string;
    customerName?: string;
    branchId?: number;
    branchName?: string;
    returnDate: string | Date;
    totalAmount: number;
    notes?: string;
    lines?: SaleReturnLineDto[];
}

export interface SaleReturnableLineDto {
    id: number;
    saleLineId: number;
    productId: number;
    productName?: string;
    soldQuantity: number;
    returnedQuantity: number;
    returnableQuantity: number;
    unitPrice: number;
}

export interface SaleReturnableDto {
    id: number;
    customerId: number;
    customerName?: string;
    invoiceNo?: string;
    saleDate: string | Date;
    lines?: SaleReturnableLineDto[];
}

export interface CreateSaleReturnLineDto {
    saleLineId: number;
    quantity: number;
}

export interface CreateSaleReturnDto {
    saleId: number;
    branchId: number;
    returnDate: string | Date;
    notes?: string;
    lines: CreateSaleReturnLineDto[];
}

export interface PagedSaleReturnResultRequestDto {
    keyword?: string;
    saleId?: number;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
