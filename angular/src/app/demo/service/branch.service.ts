import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    BranchDto,
    CreateBranchDto,
    PagedBranchResultRequestDto,
    PagedResultDto,
} from '../api/branch';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class BranchService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Branch`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedBranchResultRequestDto
    ): Promise<PagedResultDto<BranchDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.isActive !== undefined) {
            params.IsActive = input.isActive;
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
        const result = this.unwrap(res, 'Failed to load branches');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapBranch(item)
            ),
            totalCount,
        };
    }

    async getLookup(): Promise<BranchDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetLookup`)
        );
        const result = this.unwrap(res, 'Failed to load branches');
        const items = result.items || result.Items || result || [];
        return (Array.isArray(items) ? items : []).map((item: any) =>
            this.mapBranch(item)
        );
    }

    async get(id: number): Promise<BranchDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapBranch(this.unwrap(res, 'Failed to load branch'));
    }

    async create(input: CreateBranchDto): Promise<BranchDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                name: input.name,
                code: input.code,
                isActive: input.isActive !== false,
            })
        );
        return this.mapBranch(this.unwrap(res, 'Failed to create branch'));
    }

    async update(input: BranchDto): Promise<BranchDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                name: input.name,
                code: input.code,
                isActive: input.isActive,
                isDefault: input.isDefault,
            })
        );
        return this.mapBranch(this.unwrap(res, 'Failed to update branch'));
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
        this.unwrap(res, 'Failed to delete branch');
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

    private mapBranch(item: any): BranchDto {
        return {
            id: item.id ?? item.Id,
            name: item.name ?? item.Name,
            code: item.code ?? item.Code,
            isActive: item.isActive ?? item.IsActive ?? true,
            isDefault: item.isDefault ?? item.IsDefault ?? false,
        };
    }
}
