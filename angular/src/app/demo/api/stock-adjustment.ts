export enum StockAdjustmentReasons {
    Opening = 0,
    Damage = 1,
    Loss = 2,
    Recount = 3,
    Other = 4,
}

export interface StockAdjustmentLineDto {
    id: number;
    stockAdjustmentId: number;
    productId: number;
    productName?: string;
    quantityChange: number;
}

export interface StockAdjustmentDto {
    id: number;
    branchId?: number;
    branchName?: string;
    adjustmentDate: string | Date;
    reason: number;
    referenceNo?: string;
    notes?: string;
    lines?: StockAdjustmentLineDto[];
}

export interface CreateStockAdjustmentLineDto {
    productId: number;
    quantityChange: number;
}

export interface CreateStockAdjustmentDto {
    branchId: number;
    adjustmentDate: string | Date;
    reason: number;
    notes?: string;
    lines: CreateStockAdjustmentLineDto[];
}

export interface PagedStockAdjustmentResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
