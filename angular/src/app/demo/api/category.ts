export interface CategoryDto {
    id: number;
    branchId?: number;
    departmentId?: number | null;
    departmentName?: string;
    name: string;
    description?: string;
    imagePath?: string;
    imageBase64?: string;
    defaultUnitId?: number;
    defaultUnitName?: string;
}

export interface CreateCategoryDto {
    name: string;
    departmentId?: number | null;
    description?: string;
    imageBase64?: string;
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
