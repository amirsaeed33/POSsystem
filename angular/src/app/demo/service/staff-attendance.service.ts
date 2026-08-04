import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PagedResultDto } from '../api/staff';
import {
    CreateStaffAttendanceDto,
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
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId,
            branchName: item.branchName ?? item.BranchName,
            staffId: item.staffId ?? item.StaffId,
            staffName: item.staffName ?? item.StaffName,
            attendanceDate: item.attendanceDate ?? item.AttendanceDate,
            checkInTime: item.checkInTime ?? item.CheckInTime ?? null,
            checkOutTime: item.checkOutTime ?? item.CheckOutTime ?? null,
            status: item.status ?? item.Status ?? 0,
            workingHours: item.workingHours ?? item.WorkingHours ?? null,
            remarks: item.remarks ?? item.Remarks,
        };
    }
}
