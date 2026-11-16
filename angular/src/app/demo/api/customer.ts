export enum CustomerType {
    Direct = 0,
    Wholesaler = 1,
}

export interface CustomerDto {
    id: number;
    branchId: number;
    name: string;
    customerType: number;
    phone?: string;
    email?: string;
    address?: string;
    description?: string;
    /** Relative path under wwwroot/CustomerImages when a profile photo is saved. */
    imagePath?: string | null;
    accountId?: number;
    balance: number;
    /** Storefront + POS Online/Delivery order count for this customer. */
    onlineOrderCount?: number;
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
