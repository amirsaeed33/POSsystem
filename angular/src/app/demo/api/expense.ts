export interface ExpenseDto {
    id: number;
    expenseDate: string | Date;
    amount: number;
    referenceNo?: string;
    description?: string;
    paymentAccountId: number;
    paymentAccountName?: string;
    expenseAccountId: number;
    expenseAccountName?: string;
    createdByName?: string;
}

export interface CreateExpenseDto {
    expenseDate: string | Date;
    amount: number;
    referenceNo?: string;
    description?: string;
    paymentAccountId: number;
}

export interface PagedExpenseResultRequestDto {
    keyword?: string;
    paymentAccountId?: number;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
