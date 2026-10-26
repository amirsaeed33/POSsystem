import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PagedResultDto } from '../api/staff';
import {
    CreateStaffAttendanceDto,
    DailyAttendanceSheetItemDto,
    GetDailyAttendanceSheetInput,
    PagedStaffAttendanceResultRequestDto,
    StaffAttendanceDto,
} from '../api/staff-attendance';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class StaffAttendanceService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/StaffAttendance`;

    constructor(private http: HttpClient) {}

    async getDailySheet(
        input: GetDailyAttendanceSheetInput
    ): Promise<DailyAttendanceSheetItemDto[]> {
        const params: any = {
            Date: input.date,
        };
        if (input.staffId != null) {
            params.StaffId = input.staffId;
        }
        if (input.keyword) {
            params.Keyword = input.keyword;
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetDailySheet`, { params })
        );
        const result = this.unwrap(res, 'Failed to load attendance sheet');
        const items = result.items || result.Items || [];
        return (Array.isArray(items) ? items : []).map((item: any) =>
            this.mapDailySheetItem(item)
        );
    }

    async getAll(
        input?: PagedStaffAttendanceResultRequestDto
    ): Promise<PagedResultDto<StaffAttendanceDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.staffId != null) {
            params.StaffId = input.staffId;
        }
        if (input?.status != null) {
            params.Status = input.status;
        }
        if (input?.fromDate) {
            params.FromDate = input.fromDate;
        }
        if (input?.toDate) {
            params.ToDate = input.toDate;
        }
        if (input?.skipCount !== undefined) {
            params.SkipCount = input.skipCount;
        }
        if (input?.maxResultCount !== undefined) {
            params.MaxResultCount = input.maxResultCount;
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load attendance');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapAttendance(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<StaffAttendanceDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapAttendance(this.unwrap(res, 'Failed to load attendance'));
    }

    async create(input: CreateStaffAttendanceDto): Promise<StaffAttendanceDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, this.toPayload(input))
        );
        return this.mapAttendance(this.unwrap(res, 'Failed to create attendance'));
    }

    async update(input: StaffAttendanceDto): Promise<StaffAttendanceDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                branchId: input.branchId,
                ...this.toPayload(input),
                workingHours: input.workingHours,
            })
        );
        return this.mapAttendance(this.unwrap(res, 'Failed to update attendance'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to delete attendance');
    }

    private toPayload(input: CreateStaffAttendanceDto | StaffAttendanceDto): any {
        return {
            staffId: input.staffId,
            attendanceDate: input.attendanceDate,
            checkInTime: input.checkInTime || null,
            checkOutTime: input.checkOutTime || null,
            status: input.status,
            remarks: input.remarks,
        };
    }

    private unwrap(res: any, fallbackMessage: string): any {
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallbackMessage
            );
        }
        return res.result ?? res;
    }

    private mapAttendance(item: any): StaffAttendanceDto {
        const rawStatus = item.status ?? item.Status ?? 0;
        const status =
            typeof rawStatus === 'string'
                ? this.mapStatusName(rawStatus)
                : Number(rawStatus);

        return {
            id: Number(item.id ?? item.Id),
            branchId: Number(item.branchId ?? item.BranchId ?? 0),
            branchName: item.branchName ?? item.BranchName,
            staffId: Number(item.staffId ?? item.StaffId),
            staffName: item.staffName ?? item.StaffName,
            attendanceDate: item.attendanceDate ?? item.AttendanceDate,
            checkInTime: item.checkInTime ?? item.CheckInTime ?? null,
            checkOutTime: item.checkOutTime ?? item.CheckOutTime ?? null,
            status: status as any,
            workingHours: item.workingHours ?? item.WorkingHours ?? null,
            remarks: item.remarks ?? item.Remarks,
        };
    }

    private mapStatusName(name: string): number {
        switch ((name || '').toLowerCase()) {
            case 'present':
                return 0;
            case 'absent':
                return 1;
            case 'leave':
                return 2;
            case 'halfday':
            case 'half day':
                return 3;
            default:
                return 0;
        }
    }

    private mapDailySheetItem(item: any): DailyAttendanceSheetItemDto {
        const rawStatus = item.status ?? item.Status;
        let status: number | null = null;
        if (rawStatus !== null && rawStatus !== undefined) {
            status =
                typeof rawStatus === 'string'
                    ? this.mapStatusName(rawStatus)
                    : Number(rawStatus);
        }

        const hasAttendance = !!(
            item.hasAttendance ??
            item.HasAttendance ??
            item.attendanceId ??
            item.AttendanceId
        );

        return {
            id: Number(item.id ?? item.Id ?? 0),
            staffId: Number(item.staffId ?? item.StaffId),
            staffName: item.staffName ?? item.StaffName,
            designation: item.designation ?? item.Designation,
            branchName: item.branchName ?? item.BranchName,
            branchId: item.branchId ?? item.BranchId ?? null,
            attendanceDate: item.attendanceDate ?? item.AttendanceDate,
            hasAttendance,
            attendanceId: item.attendanceId ?? item.AttendanceId ?? null,
            checkInTime: item.checkInTime ?? item.CheckInTime ?? null,
            checkOutTime: item.checkOutTime ?? item.CheckOutTime ?? null,
            status: status as any,
            workingHours: item.workingHours ?? item.WorkingHours ?? null,
            remarks: item.remarks ?? item.Remarks,
        };
    }
}
