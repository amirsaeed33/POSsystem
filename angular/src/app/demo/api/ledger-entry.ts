export interface LedgerEntryDto {
    id: number;
    accountId: number;
    accountName?: string;
    transactionDate: string | Date;
    voucherType: string;
    voucherId?: number;
    debit: number;
    credit: number;
    description?: string;
}

export interface CreateLedgerEntryDto {
    accountId: number;
    transactionDate: string | Date;
    voucherType: string;
    voucherId?: number;
    debit: number;
    credit: number;
    description?: string;
}

export interface PagedLedgerEntryResultRequestDto {
    keyword?: string;
    accountId?: number;
    fromDate?: string | Date;
    toDate?: string | Date;
    voucherType?: string;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
