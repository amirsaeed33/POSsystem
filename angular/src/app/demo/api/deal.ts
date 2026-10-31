export interface DealDetailDto {
    id: number;
    dealId: number;
    productId: number;
    productName?: string;
    productBarcode?: string;
    quantity: number;
    unitPrice: number;
    subTotal?: number;
}

export interface DealDto {
    id: number;
    branchId: number;
    branchName?: string;
    name: string;
    description?: string;
    dealPrice: number;
    expiryDate?: string | Date;
    isExpired?: boolean;
    categoryType?: string;
    categoryId?: number;
    categoryName?: string;
    isActive?: boolean;
    productId?: number;
    imagePath?: string;
    items: DealDetailDto[];
    totalOriginalPrice?: number;
}

export interface CreateDealDetailDto {
    productId: number;
    quantity: number;
    unitPrice?: number;
}

export interface CreateDealDto {
    branchId?: number;
    name: string;
    description?: string;
    dealPrice: number;
    expiryDate?: string | Date;
    categoryId?: number;
    categoryType?: string;
    imageBase64?: string;
    items: CreateDealDetailDto[];
}

export interface UpdateDealDto extends CreateDealDto {
    id: number;
}

export interface PagedDealResultRequestDto {
    keyword?: string;
    branchId?: number;
    skipCount?: number;
    maxResultCount?: number;
}
