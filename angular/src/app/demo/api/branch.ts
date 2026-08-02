export interface BranchDto {
    id: number;
    name: string;
    code: string;
    isActive: boolean;
    isDefault: boolean;
}

export interface CreateBranchDto {
    name: string;
    code: string;
    isActive?: boolean;
}

export interface PagedBranchResultRequestDto {
    keyword?: string;
    isActive?: boolean;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
