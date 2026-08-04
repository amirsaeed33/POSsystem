export enum AttendanceStatus {
    Present = 0,
    Absent = 1,
    Leave = 2,
    HalfDay = 3,
}

export interface StaffAttendanceDto {
    id: number;
    branchId: number;
    branchName?: string;
    staffId: number;
    staffName?: string;
    attendanceDate: string | Date;
    checkInTime?: string | Date | null;
    checkOutTime?: string | Date | null;
    status: AttendanceStatus;
    workingHours?: number | null;
    remarks?: string;
}

export interface CreateStaffAttendanceDto {
    staffId: number;
    attendanceDate: string | Date;
    checkInTime?: string | Date | null;
    checkOutTime?: string | Date | null;
    status: AttendanceStatus;
    remarks?: string;
}

export interface PagedStaffAttendanceResultRequestDto {
    keyword?: string;
    staffId?: number | null;
    status?: AttendanceStatus | null;
    fromDate?: string | null;
    toDate?: string | null;
    skipCount?: number;
    maxResultCount?: number;
}
