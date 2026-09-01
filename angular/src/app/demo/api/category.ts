export interface CategoryDto {
    id: number;
    branchId?: number;
    name: string;
    description?: string;
    defaultUnitId?: number;
    defaultUnitName?: string;
}

export interface CreateCategoryDto {
    name: string;
    description?: string;
    defaultUnitId?: number;
}

export interface PagedCategoryResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
