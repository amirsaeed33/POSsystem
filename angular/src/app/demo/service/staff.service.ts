import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    ChangeStaffLoginPasswordDto,
    CreateStaffDto,
    CreateStaffLoginDto,
    PagedResultDto,
    PagedStaffResultRequestDto,
    StaffDto,
} from '../api/staff';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class StaffService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Staff`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedStaffResultRequestDto
    ): Promise<PagedResultDto<StaffDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.isActive !== undefined && input?.isActive !== null) {
            params.IsActive = input.isActive;
        }
        if (input?.branchId !== undefined && input?.branchId !== null) {
            params.BranchId = input.branchId;
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
        const result = this.unwrap(res, 'Failed to load staff');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapStaff(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<StaffDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapStaff(this.unwrap(res, 'Failed to load staff'));
    }

    async create(input: CreateStaffDto): Promise<StaffDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, this.toPayload(input))
        );
        return this.mapStaff(this.unwrap(res, 'Failed to create staff'));
    }

    async update(input: StaffDto): Promise<StaffDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                ...this.toPayload(input),
            })
        );
        return this.mapStaff(this.unwrap(res, 'Failed to update staff'));
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
        this.unwrap(res, 'Failed to delete staff');
    }

    async createLogin(input: CreateStaffLoginDto): Promise<StaffDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/CreateLogin`, {
                staffId: input.staffId,
                email: input.email,
                password: input.password,
            })
        );
        return this.mapStaff(this.unwrap(res, 'Failed to create staff login'));
    }

    async changeLoginPassword(
        input: ChangeStaffLoginPasswordDto
    ): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/ChangeLoginPassword`, {
                staffId: input.staffId,
                newPassword: input.newPassword,
            })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to change staff password');
    }

    private toPayload(input: CreateStaffDto | StaffDto): any {
        return {
            branchId: input.branchId ?? null,
            employeeCode: input.employeeCode,
            name: input.name,
            phone: input.phone,
            email: input.email,
            address: input.address,
            designation: input.designation,
            joiningDate: input.joiningDate,
            basicSalary: input.basicSalary ?? null,
            isActive: input.isActive,
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

    private mapStaff(item: any): StaffDto {
        const userId = item.userId ?? item.UserId ?? null;
        const hasUserAccount =
            item.hasUserAccount ?? item.HasUserAccount ?? !!userId;
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId ?? null,
            branchName: item.branchName ?? item.BranchName,
            employeeCode: item.employeeCode ?? item.EmployeeCode,
            name: item.name ?? item.Name,
            phone: item.phone ?? item.Phone,
            email: item.email ?? item.Email,
            address: item.address ?? item.Address,
            designation: item.designation ?? item.Designation,
            joiningDate: item.joiningDate ?? item.JoiningDate,
            basicSalary: item.basicSalary ?? item.BasicSalary ?? null,
            isActive: item.isActive ?? item.IsActive ?? true,
            userId,
            hasUserAccount,
        };
    }
}
