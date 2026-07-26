export interface SupplierDto {
    id: number;
    name: string;
    phone?: string;
    email?: string;
    address?: string;
    description?: string;
    accountId?: number;
    balance: number;
}

export interface CreateSupplierDto {
    name: string;
    phone?: string;
    email?: string;
    address?: string;
    description?: string;
}

export interface PagedSupplierResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
