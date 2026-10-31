import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    BrandDto,
    CreateBrandDto,
    PagedBrandResultRequestDto,
    PagedResultDto,
} from '../api/brand';
import { environment } from '../../../environments/environment';
import { TtlCache, branchCacheKey } from './ttl-cache';

@Injectable({
    providedIn: 'root',
})
export class BrandService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Brand`;
    private readonly lookupCache = new TtlCache<any[]>(5 * 60 * 1000);

    constructor(private http: HttpClient) {}

        async getAll(
        input?: PagedBrandResultRequestDto
    ): Promise<PagedResultDto<BrandDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
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
        const result = this.unwrap(res, 'Failed to load brands');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapBrand(item)
            ),
            totalCount,
        };
    }

    clearCache(): void {
        this.lookupCache.clear();
    }

    async getLookup(forceRefresh = false): Promise<BrandDto[]> {
        const key = branchCacheKey(this.readBranchId(), 'lookup');
        if (!forceRefresh) {
            const hit = this.lookupCache.get(key);
            if (hit) {
                return hit;
            }
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetLookup`)
        );
        const result = this.unwrap(res, 'Failed to load brands');
        const items = result.items || result.Items || result || [];
        const mapped = (Array.isArray(items) ? items : []).map((item: any) =>
            this.mapBrand(item)
        );
        this.lookupCache.set(key, mapped);
        return mapped;
    }

    private readBranchId(): number | null {
        const n = parseInt(localStorage.getItem('SmartPos.BranchId') || '', 10);
        return Number.isFinite(n) && n > 0 ? n : null;
    }

    async get(id: number): Promise<BrandDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapBrand(this.unwrap(res, 'Failed to load brand'));
    }

    async create(input: CreateBrandDto): Promise<BrandDto> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                name: input.name,
                description: input.description,
            })
        );
        return this.mapBrand(this.unwrap(res, 'Failed to create brand'));
    }

    async update(input: BrandDto): Promise<BrandDto> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                branchId: input.branchId,
                name: input.name,
                description: input.description,
            })
        );
        return this.mapBrand(this.unwrap(res, 'Failed to update brand'));
    }

    async delete(id: number): Promise<void> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to delete brand');
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

    private mapBrand(item: any): BrandDto {
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId,
            name: item.name ?? item.Name,
            description: item.description ?? item.Description,
        };
    }
}
