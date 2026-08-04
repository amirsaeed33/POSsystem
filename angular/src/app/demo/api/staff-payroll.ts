export enum PayrollPaymentStatus {
    Pending = 0,
    Paid = 1,
    Cancelled = 2,
}

export interface StaffPayrollDto {
    id: number;
    branchId: number;
    branchName?: string;
    staffId: number;
    staffName?: string;
    month: number;
    year: number;
    basicSalary: number;
    allowance: number;
    bonus: number;
    deduction: number;
    overtimeAmount: number;
    netSalary: number;
    paymentStatus: PayrollPaymentStatus;
    paymentDate?: string | Date | null;
    remarks?: string;
}

export interface CreateStaffPayrollDto {
    staffId: number;
    month: number;
    year: number;
    basicSalary?: number | null;
    allowance: number;
    bonus: number;
    deduction: number;
    overtimeAmount: number;
    paymentStatus: PayrollPaymentStatus;
    paymentDate?: string | Date | null;
    remarks?: string;
}

export interface PagedStaffPayrollResultRequestDto {
    keyword?: string;
    staffId?: number | null;
    month?: number | null;
    year?: number | null;
    paymentStatus?: PayrollPaymentStatus | null;
    skipCount?: number;
    maxResultCount?: number;
}
