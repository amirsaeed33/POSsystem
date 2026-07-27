export enum CustomerType {
    Direct = 0,
    Wholesaler = 1,
}

export interface CustomerDto {
    id: number;
    name: string;
    customerType: number;
    phone?: string;
    email?: string;
    address?: string;
    description?: string;
    accountId?: number;
    balance: number;
}

export interface CreateCustomerDto {
    name: string;
    customerType: number;
    phone?: string;
    email?: string;
    address?: string;
    description?: string;
}

export interface PagedCustomerResultRequestDto {
    keyword?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
