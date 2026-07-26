export interface BrandDto {
    id: number;
    name: string;
    description?: string;
}

export interface CreateBrandDto {
    name: string;
    description?: string;
}

export interface PagedBrandResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
