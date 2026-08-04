export interface StaffDto {
    id: number;
    branchId?: number | null;
    branchName?: string;
    employeeCode?: string;
    name: string;
    phone?: string;
    email?: string;
    address?: string;
    designation?: string;
    joiningDate: string | Date;
    basicSalary?: number | null;
    isActive: boolean;
}

export interface CreateStaffDto {
    branchId?: number | null;
    employeeCode?: string;
    name: string;
    phone?: string;
    email?: string;
    address?: string;
    designation?: string;
    joiningDate: string | Date;
    basicSalary?: number | null;
    isActive: boolean;
}

export interface PagedStaffResultRequestDto {
    keyword?: string;
    isActive?: boolean | null;
    branchId?: number | null;
    skipCount?: number;
    maxResultCount?: number;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
}
