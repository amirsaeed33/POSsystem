// Verona UI Kit demo types
interface InventoryStatus {
    label: string;
    value: string;
}

export interface Product {
    id?: string;
    code?: string;
    name?: string;
    description?: string;
    price?: number;
    quantity?: number;
    inventoryStatus?: InventoryStatus;
    category?: string;
    image?: string;
    rating?: number;
}

// POS / ABP Product API types
export interface ProductDto {
    id: number;
    name: string;
    description?: string;
    location?: string;
    barcode: string;
    price: number;
    wholesalePrice: number;
    costPrice: number;
    profitPerUnit: number;
    profitMarginPercent?: number;
    stockProfit: number;
    stockQuantity: number;
    alertQuantityLimit: number;
    categoryId: number;
    categoryName?: string;
    brandId: number;
    brandName?: string;
    unitId: number;
    unitName?: string;
    branchId?: number;
    branchName?: string;
    imagePath?: string;
    imageBase64?: string;
    /** Computed: true when tenant-level (all locations). */
    isShared?: boolean;
    branchIds?: number[];
}

export interface CreateProductDto {
    name: string;
    description?: string;
    location?: string;
    barcode: string;
    price: number;
    wholesalePrice: number;
    costPrice: number;
    alertQuantityLimit: number;
    stockQuantity?: number;
    categoryId: number;
    brandId: number;
    unitId: number;
    branchId?: number;
    imageBase64?: string;
    /** Empty = tenant-level (all locations). */
    branchIds?: number[];
}

export interface PagedProductResultRequestDto {
    keyword?: string;
    categoryId?: number;
    brandId?: number;
    unitId?: number;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
