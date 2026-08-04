import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PagedResultDto } from '../api/staff';
import {
    CreateStaffPayrollDto,
    PagedStaffPayrollResultRequestDto,
    StaffPayrollDto,
} from '../api/staff-payroll';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class StaffPayrollService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/StaffPayroll`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedStaffPayrollResultRequestDto
    ): Promise<PagedResultDto<StaffPayrollDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.staffId != null) {
            params.StaffId = input.staffId;
        }
        if (input?.month != null) {
            params.Month = input.month;
        }
        if (input?.year != null) {
            params.Year = input.year;
        }
        if (input?.paymentStatus != null) {
            params.PaymentStatus = input.paymentStatus;
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
        const result = this.unwrap(res, 'Failed to load payroll');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapPayroll(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<StaffPayrollDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapPayroll(this.unwrap(res, 'Failed to load payroll'));
    }

    async create(input: CreateStaffPayrollDto): Promise<StaffPayrollDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, this.toPayload(input))
        );
        return this.mapPayroll(this.unwrap(res, 'Failed to create payroll'));
    }

    async update(input: StaffPayrollDto): Promise<StaffPayrollDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                branchId: input.branchId,
                ...this.toPayload(input),
                netSalary: input.netSalary,
            })
        );
        return this.mapPayroll(this.unwrap(res, 'Failed to update payroll'));
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
        this.unwrap(res, 'Failed to delete payroll');
    }

    private toPayload(input: CreateStaffPayrollDto | StaffPayrollDto): any {
        return {
            staffId: input.staffId,
            month: input.month,
            year: input.year,
            basicSalary: input.basicSalary ?? null,
            allowance: input.allowance ?? 0,
            bonus: input.bonus ?? 0,
            deduction: input.deduction ?? 0,
            overtimeAmount: input.overtimeAmount ?? 0,
            paymentStatus: input.paymentStatus,
            paymentDate: input.paymentDate || null,
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

    private mapPayroll(item: any): StaffPayrollDto {
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId,
            branchName: item.branchName ?? item.BranchName,
            staffId: item.staffId ?? item.StaffId,
            staffName: item.staffName ?? item.StaffName,
            month: item.month ?? item.Month,
            year: item.year ?? item.Year,
            basicSalary: item.basicSalary ?? item.BasicSalary ?? 0,
            allowance: item.allowance ?? item.Allowance ?? 0,
            bonus: item.bonus ?? item.Bonus ?? 0,
            deduction: item.deduction ?? item.Deduction ?? 0,
            overtimeAmount: item.overtimeAmount ?? item.OvertimeAmount ?? 0,
            netSalary: item.netSalary ?? item.NetSalary ?? 0,
            paymentStatus: item.paymentStatus ?? item.PaymentStatus ?? 0,
            paymentDate: item.paymentDate ?? item.PaymentDate ?? null,
            remarks: item.remarks ?? item.Remarks,
        };
    }
}