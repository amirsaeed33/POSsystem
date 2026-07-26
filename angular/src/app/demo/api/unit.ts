export interface UnitDto {
    id: number;
    name: string;
    description?: string;
}

export interface CreateUnitDto {
    name: string;
    description?: string;
}

export interface PagedUnitResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
