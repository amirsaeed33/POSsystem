export interface DepartmentDto {
    id: number;
    branchId?: number;
    name: string;
    description?: string;
    isActive: boolean;
}

export interface CreateDepartmentDto {
    name: string;
    description?: string;
    isActive: boolean;
}

export interface PagedDepartmentResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

