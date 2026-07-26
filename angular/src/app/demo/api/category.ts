export interface CategoryDto {
    id: number;
    name: string;
    description?: string;
}

export interface CreateCategoryDto {
    name: string;
    description?: string;
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
