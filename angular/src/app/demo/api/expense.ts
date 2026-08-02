export interface ExpenseDto {
    id: number;
    branchId?: number;
    branchName?: string;
    expenseDate: string | Date;
    amount: number;
    referenceNo?: string;
    description?: string;
    paymentAccountId: number;
    paymentAccountName?: string;
    expenseAccountId: number;
    expenseAccountName?: string;
}

export interface CreateExpenseDto {
    branchId: number;
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
