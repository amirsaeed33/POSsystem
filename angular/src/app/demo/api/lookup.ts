export const LookUpTypes = {
    LookUpType: 'LookUpType',
    PaymentMethod: 'PaymentMethod',
    DiscountType: 'DiscountType',
    StockAdjustmentReason: 'StockAdjustmentReason',
    Gender: 'Gender',
    BranchStatus: 'BranchStatus',
    AccountType: 'AccountType',
} as const;

export type LookUpType = (typeof LookUpTypes)[keyof typeof LookUpTypes];

export interface LookUpDto {
    id: number;
    type: string;
    name: string;
    displayName: string;
    sortOrder: number;
    isActive: boolean;
}

export interface CreateLookUpDto {
    type: string;
    name: string;
    displayName: string;
    sortOrder: number;
    isActive: boolean;
}

export interface PagedLookUpResultRequestDto {
    keyword?: string;
    type?: string;
    isActive?: boolean;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
