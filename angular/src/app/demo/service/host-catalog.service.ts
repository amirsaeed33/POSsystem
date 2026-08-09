import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateHostCatalogItemDto,
    HostCatalogByCompanyTypeDto,
    HostCatalogItemDto,
    PagedHostCatalogItemResultRequestDto,
    PagedResultDto,
} from '../api/host-catalog';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class HostCatalogService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/HostCatalogItem`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedHostCatalogItemResultRequestDto
    ): Promise<PagedResultDto<HostCatalogItemDto>> {
        const params: any = {};
        if (input?.keyword) params.Keyword = input.keyword;
        if (input?.type) params.Type = input.type;
        if (input?.companyTypeId != null) params.CompanyTypeId = input.companyTypeId;
        if (input?.isActive != null) params.IsActive = input.isActive;
        if (input?.skipCount !== undefined) params.SkipCount = input.skipCount;
        if (input?.maxResultCount !== undefined)
            params.MaxResultCount = input.maxResultCount;

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load host catalog');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((i) => this.map(i)),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async get(id: number): Promise<HostCatalogItemDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.map(this.unwrap(res, 'Failed to load item'));
    }

    async create(input: CreateHostCatalogItemDto): Promise<HostCatalogItemDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, input)
        );
        return this.map(this.unwrap(res, 'Failed to create item'));
    }

    async update(input: HostCatalogItemDto): Promise<HostCatalogItemDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, input)
        );
        return this.map(this.unwrap(res, 'Failed to update item'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete item');
    }

    async getCompanyTypesForSeed(): Promise<HostCatalogItemDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetCompanyTypesForSeed`)
        );
        const result = this.unwrap(res, 'Failed to load company types');
        const items = result.items || result.Items || [];
        return (Array.isArray(items) ? items : []).map((i) => this.map(i));
    }

    async getCatalogByCompanyType(
        companyTypeId: number
    ): Promise<HostCatalogByCompanyTypeDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetCatalogByCompanyType`, {
                params: { Id: companyTypeId },
            })
        );
        const result = this.unwrap(res, 'Failed to load catalog');
        return {
            companyType: this.map(result.companyType || result.CompanyType || {}),
            categories: (result.categories || result.Categories || []).map(
                (i: any) => this.map(i)
            ),
            units: (result.units || result.Units || []).map((i: any) =>
                this.map(i)
            ),
            brands: (result.brands || result.Brands || []).map((i: any) =>
                this.map(i)
            ),
        };
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallback
            );
        }
        return res.result ?? res;
    }

    private map(item: any): HostCatalogItemDto {
        return {
            id: item.id ?? item.Id ?? 0,
            type: item.type ?? item.Type ?? '',
            companyTypeId: item.companyTypeId ?? item.CompanyTypeId ?? null,
            companyTypeName: item.companyTypeName ?? item.CompanyTypeName,
            name: item.name ?? item.Name ?? '',
            symbol: item.symbol ?? item.Symbol,
            isActive: item.isActive ?? item.IsActive ?? true,
        };
    }
}
