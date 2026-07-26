// Verona UI Kit demo types (used by tabledemo)
export interface Country {
    name?: string;
    code?: string;
}

export interface Representative {
    name?: string;
    image?: string;
}

export interface Customer {
    id?: number;
    name?: string;
    country?: Country;
    company?: string;
    date?: string;
    status?: string;
    activity?: number;
    representative?: Representative;
}

// POS / ABP Customer API types
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
