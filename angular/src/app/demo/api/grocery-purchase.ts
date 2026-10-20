export interface GroceryPurchaseDto {
    id: number;
    creationTime?: string;
    branchId: number;
    branchName?: string;
    productName: string;
    purchasePrice: number;
    unit: string;
    unitId?: number | null;
    purchaseDate: string | Date;
    createdByName?: string;
}

export interface CreateGroceryPurchaseDto {
    branchId?: number | null;
    productName: string;
    purchasePrice: number;
    unit: string;
    unitId?: number | null;
    purchaseDate: string | Date;
}

export interface UpdateGroceryPurchaseDto {
    id: number;
    productName: string;
    purchasePrice: number;
    unit: string;
    unitId?: number | null;
    purchaseDate: string | Date;
}

export interface PagedGroceryPurchaseResultRequestDto {
    keyword?: string;
    branchId?: number;
    startDate?: string | Date;
    endDate?: string | Date;
    skipCount?: number;
    maxResultCount?: number;
}
