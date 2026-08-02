export interface PurchaseReturnLineDto {
    id: number;
    purchaseReturnId: number;
    purchaseLineId: number;
    productId: number;
    productName?: string;
    quantity: number;
    unitCost: number;
    lineTotal: number;
}

export interface PurchaseReturnDto {
    id: number;
    purchaseId: number;
    purchaseInvoiceNo?: string;
    supplierName?: string;
    branchId?: number;
    branchName?: string;
    returnDate: string | Date;
    totalAmount: number;
    notes?: string;
    lines?: PurchaseReturnLineDto[];
}

export interface PurchaseReturnableLineDto {
    id: number;
    purchaseLineId: number;
    productId: number;
    productName?: string;
    purchasedQuantity: number;
    returnedQuantity: number;
    returnableQuantity: number;
    unitCost: number;
}

export interface PurchaseReturnableDto {
    id: number;
    supplierId: number;
    supplierName?: string;
    invoiceNo?: string;
    purchaseDate: string | Date;
    lines?: PurchaseReturnableLineDto[];
}

export interface CreatePurchaseReturnLineDto {
    purchaseLineId: number;
    quantity: number;
}

export interface CreatePurchaseReturnDto {
    purchaseId: number;
    branchId: number;
    returnDate: string | Date;
    notes?: string;
    lines: CreatePurchaseReturnLineDto[];
}

export interface PagedPurchaseReturnResultRequestDto {
    keyword?: string;
    purchaseId?: number;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
