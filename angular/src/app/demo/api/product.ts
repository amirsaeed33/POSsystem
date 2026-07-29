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
    imagePath?: string;
    imageBase64?: string;
}

export interface CreateProductDto {
    name: string;
    description?: string;
    barcode: string;
    price: number;
    wholesalePrice: number;
    costPrice: number;
    alertQuantityLimit: number;
    categoryId: number;
    brandId: number;
    unitId: number;
    imageBase64?: string;
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
