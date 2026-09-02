export interface BusinessAccountDto {
    id: number;
    name: string;
    code?: string;
    accountType?: string;
    accountTypeId?: number;
    accountTypeName?: string;
    openingBalance: number;
    balance: number;
    description?: string;
    isActive: boolean;
}

export interface CreateBusinessAccountDto {
    name: string;
    code?: string;
    accountType?: string;
    accountTypeId?: number;
    openingBalance: number;
    description?: string;
    isActive: boolean;
}

export interface PagedBusinessAccountResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
