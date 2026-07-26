import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateTenantDto,
    PagedResultDto,
    PagedTenantResultRequestDto,
    TenantDto,
} from '../api/tenant';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TenantService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Tenant`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedTenantResultRequestDto
    ): Promise<PagedResultDto<TenantDto>> {
        const params: any = {};
        if (input?.keyword) params.Keyword = input.keyword;
        if (input?.isActive !== undefined) params.IsActive = input.isActive;
        if (input?.skipCount !== undefined) params.SkipCount = input.skipCount;
        if (input?.maxResultCount !== undefined)
            params.MaxResultCount = input.maxResultCount;

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load tenants');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((i) => this.map(i)),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async get(id: number): Promise<TenantDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.map(this.unwrap(res, 'Failed to load tenant'));
    }

    async create(input: CreateTenantDto): Promise<TenantDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, input)
        );
        return this.map(this.unwrap(res, 'Failed to create tenant'));
    }

    async update(input: TenantDto): Promise<TenantDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                tenancyName: input.tenancyName,
                name: input.name,
                isActive: input.isActive,
            })
        );
        return this.map(this.unwrap(res, 'Failed to update tenant'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete tenant');
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }

    private map(item: any): TenantDto {
        return {
            id: item.id ?? item.Id,
            tenancyName: item.tenancyName ?? item.TenancyName,
            name: item.name ?? item.Name,
            isActive: item.isActive ?? item.IsActive ?? true,
        };
    }
}
