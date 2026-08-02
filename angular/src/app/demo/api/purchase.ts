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
    branchId?: number;
    branchName?: string;
    purchaseDate: string | Date;
    invoiceNo?: string;
    totalAmount: number;
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
    branchId: number;
    purchaseDate: string | Date;
    invoiceNo?: string;
    notes?: string;
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
