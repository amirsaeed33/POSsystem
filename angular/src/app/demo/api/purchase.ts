export interface PurchaseLineDto {
    id: number;
    purchaseId: number;
    productId: number;
    productName?: string;
    quantity: number;
    unitCost: number;
    lineTotal: number;
}

export interface PurchaseDto {
    id: number;
    supplierId: number;
    supplierName?: string;
    purchaseDate: string | Date;
    invoiceNo?: string;
    totalAmount: number;
    amountPaid?: number;
    paymentStatus?: string;
    dueAmount?: number;
    notes?: string;
    lines?: PurchaseLineDto[];
}

export interface CreatePurchaseLineDto {
    productId: number;
    quantity: number;
    unitCost: number;
}

export interface CreatePurchaseDto {
    supplierId: number;
    purchaseDate: string | Date;
    invoiceNo?: string;
    notes?: string;
    paymentAccountId?: number | null;
    amountPaid?: number;
    lines: CreatePurchaseLineDto[];
}

export interface PagedPurchaseResultRequestDto {
    keyword?: string;
    supplierId?: number;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
